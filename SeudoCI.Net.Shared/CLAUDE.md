# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Net.Shared project.

## Project Overview

SeudoCI.Net.Shared is a minimal shared library that defines the core networking contracts and interfaces used by the SeudoCI distributed build system's networking components. This project provides the foundational abstractions that enable consistent network service discovery and communication protocols across different parts of the system.

## Build Commands

- **Build shared networking library**: `dotnet build SeudoCI.Net.Shared/SeudoCI.Net.Shared.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Net.Shared/SeudoCI.Net.Shared.csproj`
- **Run tests**: `dotnet test` (when tests are added)

## Project Structure

This is an extremely minimal shared library containing only one interface:

- **IDiscoveryBeacon.cs**: Network discovery beacon contract

## Core Interface

### IDiscoveryBeacon

**Purpose**: Defines the contract for network service discovery beacons that represent discoverable services on the network.

#### Properties

**`Guid Guid { get; }`**
- **Type**: Read-only unique identifier
- **Purpose**: Provides a globally unique identifier for the service instance
- **Usage**: Distinguishes between different service instances, enables tracking across network changes
- **Immutability**: Read-only property suggests the GUID is set at creation and never changes

**`IPAddress Address { get; set; }`**
- **Type**: Mutable network address
- **Purpose**: Specifies the IP address where the service can be reached
- **Usage**: Updated when service moves between network interfaces or addresses
- **Mutability**: Can change if service binds to different network interfaces

**`ushort Port { get; set; }`**
- **Type**: Mutable port number (0-65535)
- **Purpose**: Specifies the TCP/UDP port where the service listens
- **Usage**: Updated if service changes its listening port
- **Validation**: ushort ensures valid port range

**`DateTime LastSeen { get; set; }`**
- **Type**: Mutable timestamp
- **Purpose**: Tracks when the service was last detected/communicated with
- **Usage**: Enables timeout detection and stale service cleanup
- **Pattern**: Updated on each successful discovery or communication

## Design Patterns

### Service Discovery Contract
The interface follows a standard service discovery pattern where:
1. **Identity**: GUID provides permanent service identity
2. **Location**: Address + Port specify current network location  
3. **Freshness**: LastSeen enables staleness detection and cleanup

### Mutable Network State
Most properties are mutable to handle dynamic networking scenarios:
- Services may change IP addresses (DHCP, interface changes)
- Services may change ports (restart, configuration changes)
- Discovery systems need to track communication freshness

### Immutable Service Identity
The GUID property is read-only, establishing that:
- Service identity is permanent across network changes
- Service instances can be tracked even as network details change
- Multiple beacons with same GUID represent the same service

## Usage Patterns

### Service Registration
```csharp
public class ServiceBeacon : IDiscoveryBeacon
{
    public Guid Guid { get; } = Guid.NewGuid();
    public IPAddress Address { get; set; }
    public ushort Port { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
```

### Service Discovery Management
```csharp
public class ServiceTracker
{
    private readonly Dictionary<Guid, IDiscoveryBeacon> _services = new();
    
    public void UpdateService(IDiscoveryBeacon beacon)
    {
        beacon.LastSeen = DateTime.UtcNow;
        _services[beacon.Guid] = beacon;
    }
    
    public void CleanupStaleServices(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        var staleServices = _services.Values
            .Where(s => s.LastSeen < cutoff)
            .ToList();
        
        foreach (var service in staleServices)
        {
            _services.Remove(service.Guid);
        }
    }
}
```

### Network Address Updates
```csharp
public void HandleAddressChange(IDiscoveryBeacon beacon, IPAddress newAddress, ushort newPort)
{
    beacon.Address = newAddress;
    beacon.Port = newPort;
    beacon.LastSeen = DateTime.UtcNow;
    // Service identity (Guid) remains unchanged
}
```

## Integration with SeudoCI System

### Current Usage
The interface is currently referenced by SeudoCI.Net but may not be fully implemented in the current mDNS-based discovery system, which uses the Makaretu.Dns library directly.

### Potential Future Usage
This interface could be used for:
1. **Alternative discovery mechanisms**: UDP broadcast, HTTP-based discovery, database-backed service registry
2. **Service health monitoring**: Track when services were last responsive
3. **Load balancing**: Select services based on freshness and availability
4. **Network topology changes**: Handle services moving between networks
5. **Service lifecycle management**: Detect and cleanup disconnected services

### SeudoCI Agent Integration
Build agents could implement this interface to:
```csharp
public class BuildAgentBeacon : IDiscoveryBeacon
{
    public Guid Guid { get; } = Guid.NewGuid();
    public IPAddress Address { get; set; }
    public ushort Port { get; set; }
    public DateTime LastSeen { get; set; }
    
    // Additional agent-specific properties
    public string AgentName { get; set; }
    public AgentCapabilities Capabilities { get; set; }
    public int ActiveBuilds { get; set; }
}
```

## Dependencies

### Framework Dependencies
- **.NET 8.0**: Target framework
- **System.Net**: For IPAddress type

### No External Dependencies
This shared library has no external package dependencies, keeping it lightweight and minimizing version conflicts.

## Design Philosophy

### Minimal Contract
The interface defines only the essential properties needed for network service discovery, avoiding over-specification that could limit implementation flexibility.

### Cross-Platform Compatibility
Uses standard .NET types (Guid, IPAddress, DateTime) that work consistently across Windows, macOS, and Linux platforms.

### Future Extensibility
The simple interface can be extended by implementers without breaking the base contract, allowing for SeudoCI-specific properties while maintaining compatibility.

### Separation of Concerns
Focuses purely on network discovery concerns, avoiding business logic specific to build agents or pipeline management.

## Potential Enhancements

### Service Health Status
```csharp
public enum ServiceStatus { Active, Degraded, Offline }
public ServiceStatus Status { get; set; }
```

### Service Capabilities
```csharp
public IDictionary<string, object> Capabilities { get; }
```

### Service Metadata
```csharp
public string ServiceName { get; set; }
public string ServiceVersion { get; set; }
```

### Network Quality Metrics
```csharp
public TimeSpan ResponseTime { get; set; }
public int FailureCount { get; set; }
```

However, the current minimal design allows implementations to add these features through composition or inheritance without modifying the shared contract.