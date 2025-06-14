
using Makaretu.Dns;

namespace SeudoCI.Net;

/// <summary>
/// Discovers build agents on the local network.
/// </summary>
public class AgentDiscoveryClient : IDisposable
{
    private readonly ServiceDiscovery _serviceDiscovery = new(new MulticastService());
    // private readonly List<Agent> _agents = [];
    
    public void Start()
    {
        Console.WriteLine("starting...");
        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;
    }

    private void OnServiceInstanceDiscovered(object? s, ServiceInstanceDiscoveryEventArgs serviceInstance)
    {
        Console.WriteLine("Discovered service instance: " + serviceInstance.ServiceInstanceName);
    }
    
    private void OnServiceInstanceShutdown(object? s, ServiceInstanceShutdownEventArgs serviceInstance)
    {
        Console.WriteLine("Service shutdown instance: " + serviceInstance.ServiceInstanceName);
    }
    
    public void Stop()
    {
        Console.WriteLine("Server Discovery Stopped");
        _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
    }

    public void Dispose()
    {
        Stop();
        _serviceDiscovery.Dispose();
    }
}