namespace SeudoCI.Net;

using Makaretu.Dns;

/// <summary>
/// Advertises an mDNS service on the local network.
/// </summary>
public class AgentDiscoveryServer(string serverName, ushort port) : IDisposable
{
    private readonly ServiceDiscovery _serviceDiscovery = new(new MulticastService());
    private readonly Guid _instanceGuid = Guid.NewGuid();

    public void Start()
    {
        // https://github.com/richardschneider/net-mdns/blob/master/Spike/Program.cs
        
        var serviceName = $"SeudoCI-{serverName.Replace(' ', '-')}";
        var service = new ServiceProfile(serviceName, "_seudoci._tcp", port);
        _serviceDiscovery.Advertise(service);
    }

    public void Stop()
    {
        _serviceDiscovery.Unadvertise();
    }

    public void Dispose()
    {
        _serviceDiscovery.Dispose();
    }
}