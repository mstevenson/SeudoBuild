using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;

namespace SeudoCI.Net;

/// <summary>
/// Listens for UDP beacon packets that are broadcast at regular intervals
/// on the local network by a UdpDiscoveryServer.
/// </summary>
public class UdpDiscoveryClient : IDiscoveryClient<UdpDiscoveryBeacon>, IDisposable
{
    private const ushort MulticastPort = 6767;
    private const string MulticastAddress = "239.17.0.1";

    private UdpClient _udpClient;
    private IPEndPoint _endPoint;
    private Thread _receiveThread;
    private Thread _pruneThread;
    private readonly ConcurrentDictionary<Guid, UdpDiscoveryBeacon> _servers = new ConcurrentDictionary<Guid, UdpDiscoveryBeacon>();

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
        _udpClient = new UdpClient();
        _endPoint = new IPEndPoint(IPAddress.Any, MulticastPort);
        _udpClient.Client.Bind(_endPoint);
        var ip = IPAddress.Parse(MulticastAddress);
        _udpClient.MulticastLoopback = true;
        _udpClient.JoinMulticastGroup(ip);

        // Run
        Console.WriteLine("Server Discovery Started: " + MulticastAddress + ":" + MulticastPort);
        IsRunning = true;
        _receiveThread = new Thread(ReceiveThread) {IsBackground = true};
        _receiveThread.Start();
        _pruneThread = new Thread(PruneThread) {IsBackground = true};
        _pruneThread.Start();
    }

    /// <summary>
    /// Stop listening for UDP discovery beacons.
    /// </summary>
    public void Stop()
    {
        Console.WriteLine("Server Discovery Stopped");
        IsRunning = false;
        _udpClient?.Close();
    }

    /// <summary>
    /// Listen for new servers.
    /// </summary>
    private void ReceiveThread()
    {
//			try {
        while (IsRunning)
        {
            // blocking
            byte[] data = _udpClient.Receive(ref _endPoint);
            var serverInfo = UdpDiscoveryBeacon.FromBytes(data);
            serverInfo.Address = _endPoint.Address;

            if (_servers.TryGetValue(serverInfo.Guid, out var response))
            {
                response.LastSeen = DateTime.Now;
            }
            else
            {
                if (_servers.TryAdd(serverInfo.Guid, serverInfo))
                {
                    ServerFound(serverInfo);
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
    private void PruneThread()
    {
        while (IsRunning)
        {
            const int timeout = 3500;

            var now = DateTime.Now;
            foreach (var kvp in _servers)
            {
                var serverInfo = kvp.Value;
                var age = now.Subtract(serverInfo.LastSeen);
                if (!(age.TotalMilliseconds >= timeout))
                {
                    continue;
                }

                if (_servers.TryRemove(kvp.Key, out _))
                {
                    ServerLost(serverInfo);
                }
            }

            Thread.Sleep(100);
        }
    }

    public void Dispose()
    {
        IsRunning = false;
        _udpClient.Close();
    }
}