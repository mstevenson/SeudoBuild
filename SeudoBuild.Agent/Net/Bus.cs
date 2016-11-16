using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NetMQ;
using NetMQ.Sockets;

namespace SeudoBuild.Agent.Net
{
    // https://github.com/NetMQ/Samples/tree/master/src/Beacon/BeaconDemo

    class Bus
    {
        // Actor Protocol
        public const string PublishCommand = "P";
        public const string GetHostAddressCommand = "GetHostAddress";
        public const string AddedNodeCommand = "AddedNode";
        public const string RemovedNodeCommand = "RemovedNode";

        // Dead nodes timeout
        readonly TimeSpan deadNodeTimeout = TimeSpan.FromSeconds(10);

        // we will use this to check if we already know about the node
        public class NodeKey
        {
            public NodeKey(string name, int port)
            {
                Name = name;
                Port = port;
                Address = $"tcp://{name}:{port}";
                HostName = Dns.GetHostEntry(name).HostName;
            }

            public string Name { get; }
            public int Port { get; }

            public string Address { get; }

            public string HostName { get; private set; }

            protected bool Equals(NodeKey other)
            {
                return string.Equals(Name, other.Name) && Port == other.Port;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((NodeKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Name?.GetHashCode() ?? 0) * 397) ^ Port;
                }
            }

            public override string ToString()
            {
                return Address;
            }
        }

        readonly int broadcastPort;

        readonly NetMQActor actor;

        PublisherSocket publisher;
        SubscriberSocket subscriber;
        NetMQBeacon beacon;
        NetMQPoller poller;
        PairSocket shim;
        readonly Dictionary<NodeKey, DateTime> nodes; // value is the last time we "saw" this node
        int randomPort;

        Bus(int broadcastPort)
        {
            this.broadcastPort = broadcastPort;
            nodes = new Dictionary<NodeKey, DateTime>();
            actor = NetMQActor.Create(RunActor);
        }

        /// <summary>
        /// Creates a new message bus actor. All communication with the bus is
        /// through the returned <see cref="NetMQActor"/>.
        /// </summary>
        public static NetMQActor Create(int broadcastPort)
        {
            Bus node = new Bus(broadcastPort);
            return node.actor;
        }

        void RunActor(PairSocket shim)
        {
            // save the shim to the class to use later
            this.shim = shim;

            // create all subscriber, publisher and beacon
            using (subscriber = new SubscriberSocket())
            using (publisher = new PublisherSocket())
            using (beacon = new NetMQBeacon())
            {
                // listen to actor commands
                shim.ReceiveReady += OnShimReady;

                // subscribe to all messages
                subscriber.Subscribe("");

                // we bind to a random port, we will later publish this port
                // using the beacon
                randomPort = subscriber.BindRandomPort("tcp://*");
                Console.WriteLine("Bus subscriber is bound to {0}", subscriber.Options.LastEndpoint);

                // listen to incoming messages from other publishers, forward them to the shim
                subscriber.ReceiveReady += OnSubscriberReady;

                // configure the beacon to listen on the broadcast port
                Console.WriteLine("Beacon is being configured to UDP port {0}", broadcastPort);
                beacon.Configure(broadcastPort);

                // publishing the random port to all other nodes
                Console.WriteLine("Beacon is publishing the Bus subscriber port {0}", randomPort);
                beacon.Publish(randomPort.ToString(), TimeSpan.FromSeconds(1));

                // Subscribe to all beacon on the port
                Console.WriteLine("Beacon is subscribing to all beacons on UDP port {0}", broadcastPort);
                beacon.Subscribe("");

                // listen to incoming beacons
                beacon.ReceiveReady += OnBeaconReady;

                // Create a timer to clear dead nodes
                NetMQTimer timer = new NetMQTimer(TimeSpan.FromSeconds(1));
                timer.Elapsed += ClearDeadNodes;

                // Create and configure the poller with all sockets and the timer
                poller = new NetMQPoller { shim, subscriber, beacon, timer };

                // signal the actor that we finished with configuration and
                // ready to work
                shim.SignalOK();

                // polling until cancelled
                poller.Run();
            }
        }

        void OnShimReady(object sender, NetMQSocketEventArgs e)
        {
            // new actor command
            string command = shim.ReceiveFrameString();

            // check if we received end shim command
            if (command == NetMQActor.EndShimMessage)
            {
                // we cancel the socket which dispose and exist the shim
                poller.Stop();
            }
            else if (command == PublishCommand)
            {
                // it is a publish command
                // we just forward everything to the publisher until end of message
                NetMQMessage message = shim.ReceiveMultipartMessage();
                publisher.SendMultipartMessage(message);
            }
            else if (command == GetHostAddressCommand)
            {
                var address = beacon.BoundTo + ":" + randomPort;
                shim.SendFrame(address);
            }
        }

        void OnSubscriberReady(object sender, NetMQSocketEventArgs e)
        {
            // we got a new message from the bus
            // let's forward everything to the shim
            NetMQMessage message = subscriber.ReceiveMultipartMessage();
            shim.SendMultipartMessage(message);
        }

        void OnBeaconReady(object sender, NetMQBeaconEventArgs e)
        {
            // we got another beacon
            // let's check if we already know about the beacon
            var message = beacon.Receive();
            int port;
            int.TryParse(message.String, out port);

            NodeKey node = new NodeKey(message.PeerHost, port);

            // check if node already exist
            if (!nodes.ContainsKey(node))
            {
                // we have a new node, let's add it and connect to subscriber
                nodes.Add(node, DateTime.Now);
                publisher.Connect(node.Address);
                shim.SendMoreFrame(AddedNodeCommand).SendFrame(node.Address);
            }
            else
            {
                //Console.WriteLine("Node {0} is not a new beacon.", node);
                nodes[node] = DateTime.Now;
            }
        }

        void ClearDeadNodes(object sender, NetMQTimerEventArgs e)
        {
            // create an array with the dead nodes
            var deadNodes = nodes.
                Where(n => DateTime.Now > n.Value + deadNodeTimeout)
                .Select(n => n.Key).ToArray();

            // remove all the dead nodes from the nodes list and disconnect from the publisher
            foreach (var node in deadNodes)
            {
                nodes.Remove(node);
                publisher.Disconnect(node.Address);
                shim.SendMoreFrame(RemovedNodeCommand).SendFrame(node.Address);
            }
        }
    }
}
