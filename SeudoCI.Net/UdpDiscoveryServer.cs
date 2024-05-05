using System.Net.Sockets;
using System.Net;

namespace SeudoCI.Net;

/// <summary>
/// Broadcasts UDP beacon packets at regular intervals that may be used
/// by other services to locate the server on the local network.
/// </summary>
public class UdpDiscoveryServer : IDisposable
{
    UdpDiscoveryBeacon server;

    private const int BroadcastDelay = 1000; // milliseconds

    private const ushort MulticastPort = 6767;
    private const string MulticastAddress = "239.17.0.1";

    private UdpClient _udpClient;
    private IPEndPoint _endPoint;
    private Thread _networkThread;

    public bool IsRunning { get; protected set; }

    public UdpDiscoveryServer(UdpDiscoveryBeacon server)
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
        _udpClient = new UdpClient();
        var ip = IPAddress.Parse(MulticastAddress);
        _endPoint = new IPEndPoint(ip, MulticastPort);
        _udpClient.MulticastLoopback = true;
        _udpClient.JoinMulticastGroup(ip);

        // Run
        //Console.WriteLine("Server beacon started: port " + multicastPort);
        IsRunning = true;
        _networkThread = new Thread(NetworkThread) { IsBackground = true };
        _networkThread.Start();
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        //Console.WriteLine("Server beacon stopped");
        IsRunning = false;
        _udpClient?.Close();
        //udpClient.Dispose();
    }

    private void NetworkThread()
    {
//			try {
        while (IsRunning)
        {
            // Broadcast server beacon to all clients on the local network
            byte[] message = UdpDiscoveryBeacon.ToBytes(server);
            _udpClient.Send(message, message.Length, _endPoint);
//				Console.WriteLine ("Broadcasted");
            Thread.Sleep(BroadcastDelay);
        }

//			} catch {}
        IsRunning = false;
    }

    public void Dispose()
    {
        IsRunning = false;
        _udpClient.Close();
    }
}