using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using SeudoBuild.Agent.Net;

namespace SeudoBuild.Agent
{
    public class BuildQueue : IDisposable
    {
        CancellationTokenSource cancelTokenSource;
        CancellationToken cancelToken;

        string agentName;
        int port;

        public BuildQueue(string agentName, int port)
        {
            this.agentName = agentName;
            this.port = port;
        }

        public void Start()
        {
            StartBeacon();

            Console.WriteLine("Starting network build queue");
            BuildConsole.IndentLevel++;
            BuildConsole.WritePlus("Agent name:  " + agentName);
            BuildConsole.WritePlus("Port:        " + port);
            BuildConsole.IndentLevel--;
            cancelTokenSource = new CancellationTokenSource();
            cancelToken = cancelTokenSource.Token;

            using (var server = new ResponseSocket())
            {
                server.Bind("tcp://*:" + port);

                while (true)
                {
                    var message = server.ReceiveFrameString();

                    Console.WriteLine("Received {0}", message);

                    // processing the request
                    Thread.Sleep(100);

                    Console.WriteLine("Sending World");
                    server.SendFrame("World");
                }
            }

            //var task = Task.Factory.StartNew(ServerTask, cancelToken, TaskCreationOptions.LongRunning, null);
        }

        void StartBeacon()
        {
            // Create a bus using broadcast port 9999
            // All communication with the bus is through the returned actor
            var actor = Bus.Create(9999);

            actor.SendFrame(Bus.GetHostAddressCommand);
            var hostAddress = actor.ReceiveFrameString();

            // beacons publish every second, so wait a little longer than that to
            // let all the other nodes connect to our new node
            Thread.Sleep(1100);

            // publish a hello message
            // note we can use NetMQSocket send and receive extension methods
            actor.SendMoreFrame(Bus.PublishCommand).SendMoreFrame("Hello?").SendFrame(hostAddress);

            // receive messages from other nodes on the bus
            while (true)
            {
                // actor is receiving messages forwarded by the Bus subscriber
                string message = actor.ReceiveFrameString();
                switch (message)
                {
                    case "Hello?":
                        // another node is saying hello
                        var fromHostAddress = actor.ReceiveFrameString();
                        var msg = fromHostAddress + " says Hello?";
                        Console.WriteLine(msg);

                        // send back a welcome message via the Bus publisher
                        msg = hostAddress + " says Welcome!";
                        actor.SendMoreFrame(Bus.PublishCommand).SendFrame(msg);
                        break;
                    case Bus.AddedNodeCommand:
                        var addedAddress = actor.ReceiveFrameString();
                        Console.WriteLine("Added node {0} to the Bus", addedAddress);
                        break;
                    case Bus.RemovedNodeCommand:
                        var removedAddress = actor.ReceiveFrameString();
                        Console.WriteLine("Removed node {0} from the Bus", removedAddress);
                        break;
                    default:
                        // it's probably a welcome message
                        Console.WriteLine(message);
                        break;
                }
            }
        }

        void ServerTask()
        {
            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    // TODO
                }
            }
            finally
            {
                // TODO cleanup
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping server");
            cancelTokenSource.Cancel();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
