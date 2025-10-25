using System;
using Makaretu.Dns;

namespace SeudoCI.Net;

/// <summary>
/// Event arguments raised when an agent is discovered, updated or removed.
/// </summary>
public sealed class AgentEventArgs : EventArgs
{
    public AgentEventArgs(DomainName serviceInstanceName, Agent agent)
    {
        ServiceInstanceName = serviceInstanceName ?? throw new ArgumentNullException(nameof(serviceInstanceName));
        Agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    public DomainName ServiceInstanceName { get; }

    public Agent Agent { get; }
}
