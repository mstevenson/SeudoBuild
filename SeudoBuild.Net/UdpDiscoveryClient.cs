using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;

namespace SeudoBuild.Net
{
    /// <summary>
    /// Listens for UDP beacon packets that are broadcast at regular intervals
    /// on the local network by a UdpDiscoveryServer.
    /// </summary>
    public class UdpDiscoveryClient : IDisposable
    {
        const ushort multicastPort = 6767;
        const string multicastAddress = "239.17.0.1";

        UdpClient udpClient;
        IPEndPoint endPoint;
        Thread receiveThread;
        Thread pruneThread;
        ConcurrentDictionary<Guid, UdpDiscoveryBeacon> servers = new ConcurrentDictionary<Guid, UdpDiscoveryBeacon>();

        /// <summary>
        /// Occurs when a beacon is first received from a UdpDiscoveryServer.
        /// </summary>
        public event Action<UdpDiscoveryBeacon> ServerFound = delegate { };

        /// <summary>
        /// Occurs when a beacon has not been received by a known
        /// UdpDiscoveryServer for some time.
        /// </summary>
        public event Action<UdpDiscoveryBeacon> ServerLost = delegate { };

        /// <summary>
        /// Indicates whether the UdpDiscoveryClient is currently listening
        /// for beacons from UdpDiscoveryServers.
        /// </summary>
        public bool IsRunning { get; protected set; }

        //public UdpDiscoveryBeacon[] AvailableServers
        //{
        //    get
        //    {
        //        if (servers.Count == 0)
        //        {
        //            return new UdpDiscoveryBeacon[0];
        //        }
        //        return servers.Values.ToArray();
        //    }
        //}

        /// <summary>
        /// Begin listening for UDP discovery beacons.
        /// </summary>
        public void Start()
        {
            // Initialize
            udpClient = new UdpClient();
            endPoint = new IPEndPoint(IPAddress.Any, multicastPort);
            udpClient.Client.Bind(endPoint);
            var ip = IPAddress.Parse(multicastAddress);
            udpClient.MulticastLoopback = true;
            udpClient.JoinMulticastGroup(ip);

            // Run
            Console.WriteLine("Server Discovery Started: " + multicastAddress + ":" + multicastPort);
            IsRunning = true;
            receiveThread = new Thread(new ThreadStart(ReceiveThread));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            pruneThread = new Thread(new ThreadStart(PruneThread));
            pruneThread.IsBackground = true;
            pruneThread.Start();
        }

        /// <summary>
        /// Stop listening for UDP discovery beacons.
        /// </summary>
        public void Stop()
        {
            Console.WriteLine("Server Discovery Stopped");
            IsRunning = false;
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        /// <summary>
        /// Listen for new servers.
        /// </summary>
        void ReceiveThread()
        {
            //			try {
            while (IsRunning)
            {
                // blocking
                byte[] data = udpClient.Receive(ref endPoint);
                UdpDiscoveryBeacon serverInfo = UdpDiscoveryBeacon.FromBytes(data);
                serverInfo.address = endPoint.Address;

                if (serverInfo != null)
                {
                    UdpDiscoveryBeacon response;
                    if (servers.TryGetValue(serverInfo.guid, out response))
                    {
                        response.lastSeen = DateTime.Now;
                    }
                    else
                    {
                        if (servers.TryAdd(serverInfo.guid, serverInfo))
                        {
                            ServerFound(serverInfo);
                        }
                    }
                }
            }
            //			} catch {}
            IsRunning = false;
        }

        /// <summary>
        /// Prune known servers that have not broadcast discovery beacons
        /// for some time.
        /// </summary>
        void PruneThread()
        {
            while (IsRunning)
            {
                const int timeout = 3500;

                var now = DateTime.Now;
                foreach (var kvp in servers)
                {
                    var serverInfo = kvp.Value;
                    var age = now.Subtract(serverInfo.lastSeen);
                    if (age.TotalMilliseconds >= timeout)
                    {
                        UdpDiscoveryBeacon removed;
                        if (servers.TryRemove(kvp.Key, out removed))
                        {
                            ServerLost(serverInfo);
                        }
                    }
                }

                Thread.Sleep(100);
            }
        }

        public void Dispose()
        {
            IsRunning = false;
            udpClient.Close();
        }

    }

}