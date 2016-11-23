using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace SeudoBuild.Net
{
    public class UdpDiscoveryServer : IDisposable
    {
        ServerInfo server;

        const int broadcastDelay = 1000; // milliseconds

        const ushort multicastPort = 6767;
        const string multicastAddress = "239.17.0.1";

        UdpClient udpClient;
        IPEndPoint endPoint;
        Thread networkThread;

        bool isInitialized;

        public bool IsRunning { get; protected set; }

        public UdpDiscoveryServer(ServerInfo server)
        {
            this.server = server;
        }

        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            // Initialize
            udpClient = new UdpClient();
            var ip = IPAddress.Parse(multicastAddress);
            endPoint = new IPEndPoint(ip, multicastPort);
            udpClient.MulticastLoopback = true;
            udpClient.JoinMulticastGroup(ip);

            // Run
            Console.WriteLine("Server beacon started: port " + multicastPort);
            IsRunning = true;
            networkThread = new Thread(new ThreadStart(NetworkThread));
            networkThread.IsBackground = true;
            networkThread.Start();
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }
            Console.WriteLine("Server beacon stopped");
            IsRunning = false;
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient.Dispose();
            }
        }

        void NetworkThread()
        {
            //			try {
            while (IsRunning)
            {
                // Broadcast server beacon to all clients on the local network
                byte[] message = ServerInfo.ToBytes(server);
                udpClient.Send(message, message.Length, endPoint);
                //				Console.WriteLine ("Broadcasted");
                Thread.Sleep(broadcastDelay);
            }
            //			} catch {}
            IsRunning = false;
        }

        public void Dispose()
        {
            IsRunning = false;
            udpClient.Close();
        }
    }

}

