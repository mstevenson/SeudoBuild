using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Makaretu.Dns;

namespace SeudoCI.Net;

/// <summary>
/// Advertises an mDNS service on the local network.
/// </summary>
public sealed class AgentDiscoveryServer : IDisposable
{
    private static readonly DomainName ServiceType = "_seudoci._tcp";
    private static readonly IPAddress[] LoopbackFallback = [IPAddress.Loopback];

    private readonly string _serverName;
    private readonly ushort _port;
    private readonly Guid _instanceGuid = Guid.NewGuid();
    private readonly MulticastService _multicastService = new();
    private readonly ServiceDiscovery _serviceDiscovery;

    private ServiceProfile? _serviceProfile;
    private bool _started;
    private bool _disposed;

    public AgentDiscoveryServer(string serverName, ushort port)
    {
        _serverName = serverName ?? throw new ArgumentNullException(nameof(serverName));
        _port = port;
        _serviceDiscovery = new ServiceDiscovery(_multicastService);
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (_started)
        {
            return;
        }

        var profile = CreateServiceProfile();
        _multicastService.Start();
        _serviceDiscovery.Advertise(profile);
        _serviceDiscovery.Announce(profile);

        _serviceProfile = profile;
        _started = true;
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        if (_serviceProfile is not null)
        {
            _serviceDiscovery.Unadvertise(_serviceProfile);
        }

        _multicastService.Stop();
        _serviceProfile = null;
        _started = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _serviceDiscovery.Dispose();
        _multicastService.Dispose();
        _disposed = true;
    }

    private ServiceProfile CreateServiceProfile()
    {
        var instanceLabel = $"SeudoCI-{SanitizeName(_serverName)}-{_instanceGuid:N}";
        var instanceName = (DomainName)instanceLabel;
        var profile = new ServiceProfile(instanceName, ServiceType, _port, GetAdvertisableAddresses());

        var txtRecord = profile.Resources.OfType<TXTRecord>().FirstOrDefault();
        if (txtRecord is not null)
        {
            txtRecord.Strings.Add($"name={_serverName}");
            txtRecord.Strings.Add($"guid={_instanceGuid}");
        }

        return profile;
    }

    private static IPAddress[] GetAdvertisableAddresses()
    {
        var addresses = MulticastService
            .GetIPAddresses()
            .Where(IsUsableAddress)
            .ToArray();

        return addresses.Length > 0 ? addresses : LoopbackFallback;
    }

    private static bool IsUsableAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            return !Equals(address, IPAddress.Any);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !Equals(address, IPAddress.IPv6None)
                && !Equals(address, IPAddress.IPv6Any)
                && !Equals(address, IPAddress.IPv6Loopback)
                && !address.IsIPv6LinkLocal
                && !address.IsIPv6Multicast;
        }

        return true;
    }

    private static string SanitizeName(string name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? "agent"
            : name.Replace(' ', '-');
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentDiscoveryServer));
        }
    }
}
