using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Makaretu.Dns;

namespace SeudoCI.Net;

/// <summary>
/// Discovers build agents on the local network.
/// </summary>
public class AgentDiscoveryClient : IDisposable
{
    private static readonly DomainName ServiceType = "_seudoci._tcp.local";

    private readonly IMulticastService _multicastService;
    private readonly ServiceDiscovery _serviceDiscovery;
    private readonly object _syncRoot = new();
    private readonly Dictionary<string, ServiceRecord> _serviceRecords = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Agent> _agents = new(StringComparer.OrdinalIgnoreCase);

    private bool _started;
    private bool _disposed;

    public AgentDiscoveryClient()
        : this(new MulticastService())
    {
    }

    internal AgentDiscoveryClient(IMulticastService multicastService)
    {
        _multicastService = multicastService ?? throw new ArgumentNullException(nameof(multicastService));
        _serviceDiscovery = new ServiceDiscovery(multicastService);
    }

    public event EventHandler<AgentEventArgs>? AgentDiscovered;
    public event EventHandler<AgentEventArgs>? AgentUpdated;
    public event EventHandler<AgentEventArgs>? AgentRemoved;

    public IReadOnlyCollection<Agent> Agents
    {
        get
        {
            lock (_syncRoot)
            {
                return _agents.Values.ToList();
            }
        }
    }

    public Agent? FindByName(string agentName)
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            return null;
        }

        lock (_syncRoot)
        {
            return _agents.Values.FirstOrDefault(a => string.Equals(a.Name, agentName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool TryGetAgent(DomainName serviceInstanceName, out Agent? agent)
    {
        lock (_syncRoot)
        {
            return _agents.TryGetValue(Normalize(serviceInstanceName), out agent);
        }
    }

    public void Start()
    {
        ThrowIfDisposed();
        if (_started)
        {
            return;
        }

        _multicastService.NetworkInterfaceDiscovered += OnNetworkInterfaceDiscovered;
        _multicastService.AnswerReceived += OnAnswerReceived;
        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;

        _multicastService.Start();
        _serviceDiscovery.QueryServiceInstances(ServiceType);

        _started = true;
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
        _multicastService.NetworkInterfaceDiscovered -= OnNetworkInterfaceDiscovered;
        _multicastService.AnswerReceived -= OnAnswerReceived;

        _multicastService.Stop();

        lock (_syncRoot)
        {
            _serviceRecords.Clear();
            _agents.Clear();
        }

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

    internal void ProcessServiceAnnouncement(Message message, DomainName serviceInstanceName)
    {
        HandleDnsMessage(message, serviceInstanceName, requestAdditionalRecords: false);
    }

    private void OnNetworkInterfaceDiscovered(object? sender, NetworkInterfaceEventArgs e)
    {
        _serviceDiscovery.QueryServiceInstances(ServiceType);
    }

    private void OnAnswerReceived(object? sender, MessageEventArgs e)
    {
        HandleDnsMessage(e.Message, null, requestAdditionalRecords: false);
    }

    private void OnServiceInstanceDiscovered(object? s, ServiceInstanceDiscoveryEventArgs e)
    {
        HandleDnsMessage(e.Message, e.ServiceInstanceName, requestAdditionalRecords: true);
    }

    private void OnServiceInstanceShutdown(object? s, ServiceInstanceShutdownEventArgs e)
    {
        lock (_syncRoot)
        {
            var key = Normalize(e.ServiceInstanceName);
            if (_serviceRecords.Remove(key, out var record) && _agents.Remove(key, out var agent))
            {
                AgentRemoved?.Invoke(this, new AgentEventArgs(record.ServiceInstanceName, agent));
            }
        }
    }

    private void HandleDnsMessage(Message message, DomainName? serviceInstanceName, bool requestAdditionalRecords)
    {
        if (message is null)
        {
            return;
        }

        lock (_syncRoot)
        {
            var touchedRecords = new HashSet<ServiceRecord>();

            if (serviceInstanceName is not null)
            {
                var record = GetOrCreateRecord(serviceInstanceName);
                record.LastSeen = DateTimeOffset.UtcNow;
                touchedRecords.Add(record);
            }

            foreach (var srv in Enumerate<SRVRecord>(message))
            {
                var record = GetOrCreateRecord(srv.Name);
                record.Port = srv.Port;
                record.TargetHost = srv.Target;
                record.TargetHostKey = Normalize(srv.Target);
                record.InstanceLabel = srv.Name.Labels.FirstOrDefault();
                touchedRecords.Add(record);
            }

            foreach (var txt in Enumerate<TXTRecord>(message))
            {
                var key = Normalize(txt.Name);
                if (_serviceRecords.TryGetValue(key, out var record))
                {
                    var displayName = ExtractDisplayName(txt);
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        record.DisplayName = displayName;
                    }

                    touchedRecords.Add(record);
                }
            }

            foreach (var address in Enumerate<AddressRecord>(message))
            {
                var addressKey = Normalize(address.Name);
                foreach (var record in _serviceRecords.Values)
                {
                    if (record.TargetHostKey is not null &&
                        string.Equals(record.TargetHostKey, addressKey, StringComparison.OrdinalIgnoreCase))
                    {
                        record.Address = address.Address;
                        touchedRecords.Add(record);
                    }
                }
            }

            foreach (var record in touchedRecords)
            {
                if (requestAdditionalRecords)
                {
                    RequestMissingRecords(record);
                }

                TryPublish(record);
            }
        }
    }

    private ServiceRecord GetOrCreateRecord(DomainName serviceInstanceName)
    {
        var key = Normalize(serviceInstanceName);
        if (!_serviceRecords.TryGetValue(key, out var record))
        {
            record = new ServiceRecord(serviceInstanceName)
            {
                ServiceKey = key
            };
            _serviceRecords[key] = record;
        }
        else
        {
            record.ServiceInstanceName = serviceInstanceName;
        }

        return record;
    }

    private void RequestMissingRecords(ServiceRecord record)
    {
        if (record.TargetHost is null || !record.Port.HasValue)
        {
            _multicastService.SendQuery(record.ServiceInstanceName, type: DnsType.SRV);
        }

        if (record.TargetHost is not null && record.Address is null)
        {
            _multicastService.SendQuery(record.TargetHost, type: DnsType.A);
            _multicastService.SendQuery(record.TargetHost, type: DnsType.AAAA);
        }
    }

    private void TryPublish(ServiceRecord record)
    {
        if (!record.IsComplete)
        {
            return;
        }

        var agentName = record.DisplayName;
        if (string.IsNullOrWhiteSpace(agentName))
        {
            agentName = record.InstanceLabel ?? record.ServiceInstanceName.ToString();
        }

        var addressText = record.Address!.ToString();
        if (record.Port.HasValue)
        {
            addressText = $"{addressText}:{record.Port.Value}";
        }

        var agent = new Agent
        {
            Name = agentName,
            Address = addressText
        };

        if (_agents.TryGetValue(record.ServiceKey, out var existing))
        {
            if (!existing.Equals(agent))
            {
                _agents[record.ServiceKey] = agent;
                record.Agent = agent;
                AgentUpdated?.Invoke(this, new AgentEventArgs(record.ServiceInstanceName, agent));
            }
        }
        else
        {
            _agents[record.ServiceKey] = agent;
            record.Agent = agent;
            AgentDiscovered?.Invoke(this, new AgentEventArgs(record.ServiceInstanceName, agent));
        }
    }

    private static IEnumerable<TRecord> Enumerate<TRecord>(Message message)
        where TRecord : ResourceRecord
    {
        return message.Answers.OfType<TRecord>().Concat(message.AdditionalRecords.OfType<TRecord>());
    }

    private static string? ExtractDisplayName(TXTRecord txtRecord)
    {
        foreach (var entry in txtRecord.Strings)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            const string Prefix = "name=";
            if (entry.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return entry[Prefix.Length..];
            }
        }

        return null;
    }

    private static string Normalize(DomainName name)
    {
        return name.ToString().TrimEnd('.');
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(AgentDiscoveryClient));
        }
    }

    private sealed class ServiceRecord
    {
        public ServiceRecord(DomainName serviceInstanceName)
        {
            ServiceInstanceName = serviceInstanceName;
        }

        public string ServiceKey { get; init; } = string.Empty;
        public DomainName ServiceInstanceName { get; set; }
        public string? InstanceLabel { get; set; }
        public DomainName? TargetHost { get; set; }
        public string? TargetHostKey { get; set; }
        public ushort? Port { get; set; }
        public IPAddress? Address { get; set; }
        public string? DisplayName { get; set; }
        public Agent? Agent { get; set; }
        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;

        public bool IsComplete => Address is not null && Port.HasValue;
    }
}
