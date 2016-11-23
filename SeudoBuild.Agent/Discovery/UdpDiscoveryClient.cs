using System;
using System.Threading;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;

namespace SeudoBuild.Net
{
    /// <summary>
    /// Listens for UDP beacon packets from UdpDiscoveryServer.
    /// </summary>
    public class UdpDiscoveryClient : IDisposable
    {
        const ushort multicastPort = 6767;
        const string multicastAddress = "239.17.0.1";

        UdpClient udpClient;
        IPEndPoint endPoint;
        Thread networkThread;
        List<ServerInfo> servers = new List<ServerInfo>();

        public event Action<ServerInfo> ServerFound;
        public event Action<ServerInfo> ServerLost;

        public bool IsRunning { get; protected set; }

        public ServerInfo[] AvailableServers
        {
            get
            {
                if (servers.Count == 0)
                {
                    return new ServerInfo[0];
                }
                else
                {
                    return servers.ToArray();
                }
            }
        }

        public void OnServerFound(ServerInfo server)
        {
            if (ServerFound != null)
            {
                ServerFound(server);
            }
        }

        public void OnServerLost(ServerInfo server)
        {
            if (ServerLost != null)
            {
                ServerLost(server);
            }
        }


        public void Initialize()
        {
            udpClient = new UdpClient();
            var localEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
            udpClient.Client.Bind(localEndPoint);
            var ip = IPAddress.Parse(multicastAddress);
            udpClient.MulticastLoopback = true;
            udpClient.JoinMulticastGroup(ip);
        }

        public void Start()
        {
            Console.WriteLine("Server Discovery Started: " + multicastAddress + ":" + multicastPort);
            IsRunning = true;
            networkThread = new Thread(new ThreadStart(NetworkThread));
            networkThread.IsBackground = true;
            networkThread.Start();
        }

        public void Stop()
        {
            Console.WriteLine("Server Discovery Stopped");
            IsRunning = false;
            if (udpClient != null)
            {
                udpClient.Close();
            }
        }

        void NetworkThread()
        {
            //			try {
            while (IsRunning)
            {
                byte[] data = udpClient.Receive(ref endPoint);
                ServerInfo serverInfo = ServerInfo.FromBytes(data);
                serverInfo.address = endPoint.Address;
                if (serverInfo != null)
                {
                    bool serverExists = false;
                    foreach (var s in servers)
                    {
                        if (s == serverInfo)
                        {
                            s.lastSeen = DateTime.Now;
                            serverExists = true;
                        }
                    }
                    if (!serverExists)
                    {
                        servers.Add(serverInfo);
                        OnServerFound(serverInfo);
                    }
                    servers = PruneLostServers(servers);
                }
            }
            //			} catch {}
            IsRunning = false;
        }

        List<ServerInfo> PruneLostServers(List<ServerInfo> servers, int timeout = 3500)
        {
            List<ServerInfo> pruned = new List<ServerInfo>();
            var now = DateTime.Now;
            foreach (var serverInfo in servers)
            {
                var span = serverInfo.lastSeen.Subtract(now);
                if (span.Milliseconds < timeout)
                {
                    pruned.Add(serverInfo);
                }
                else
                {
                    OnServerLost(serverInfo);
                }
            }
            return pruned;
        }

        public void Dispose()
        {
            IsRunning = false;
            udpClient.Close();
        }

    }

}