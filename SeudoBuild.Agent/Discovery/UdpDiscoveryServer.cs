using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Linq;

namespace BridgeSim.Net.Discovery
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

        public bool IsRunning { get; protected set; }

        public UdpDiscoveryServer(ServerInfo server)
        {
            this.server = server;
        }

		public void Initialize ()
		{
			udpClient = new UdpClient ();
			var ip = IPAddress.Parse (multicastAddress);
			endPoint = new IPEndPoint (ip, multicastPort);
			udpClient.MulticastLoopback = true;
			udpClient.JoinMulticastGroup (ip);
		}

		public void Start ()
		{
			Console.WriteLine ("Server Beacon Started: port " + multicastPort);
			IsRunning = true;
			networkThread = new Thread (new ThreadStart (NetworkThread));
			networkThread.IsBackground = true;
			networkThread.Start ();
		}

		public void Stop ()
		{
			Console.WriteLine ("Server Beacon Stopped");
			IsRunning = false;
			if (udpClient != null) {
				udpClient.Close ();
			}
		}

		void NetworkThread ()
		{
			//			try {
			while (IsRunning) {
				// Broadcast server beacon to all clients on the local network
				byte[] message = ServerInfo.ToBytes (server);
				udpClient.Send (message, message.Length, endPoint);
//				Console.WriteLine ("Broadcasted");
				Thread.Sleep (broadcastDelay);
			}
			//			} catch {}
			IsRunning = false;
		}

		public void Dispose ()
		{
			IsRunning = false;
			udpClient.Close ();
		}
	}

}

