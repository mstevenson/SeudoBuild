# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Net project.

## Project Overview

SeudoCI.Net is the networking and service discovery library for the SeudoCI distributed build system. It provides mDNS-based automatic discovery of build agents on the local network, enabling seamless distributed build coordination without manual configuration. The library handles both advertising build agent services and discovering available agents for build submission.

## Build Commands

- **Build networking library**: `dotnet build SeudoCI.Net/SeudoCI.Net.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Net/SeudoCI.Net.csproj`
- **Run tests**: `dotnet test` (when test projects reference this library)

## Architecture Overview

SeudoCI.Net implements a client-server discovery pattern using mDNS (Multicast DNS) for zero-configuration networking:

1. **AgentDiscoveryServer**: Advertises build agent availability on the network
2. **AgentDiscoveryClient**: Discovers available build agents
3. **Agent**: Data model representing discovered build agents

## Core Components

### Agent Data Model

**Location**: `Agent.cs`

**Purpose**: Represents a discovered build agent with identity and network location information.

#### Properties
- `string Name` - Human-readable agent identifier (e.g., "brilliant-dolphin")
- `string Address` - Network address for agent communication

#### Features
- **Equality comparison**: Based on both name and address for uniqueness
- **Hash code implementation**: Enables use in collections and dictionaries
- **JSON serializable**: Compatible with REST API communication

#### Usage Pattern
```csharp
var agent = new Agent 
{ 
    Name = "brilliant-dolphin", 
    Address = "192.168.1.100:5511" 
};
```

### AgentDiscoveryServer

**Location**: `AgentDiscoveryServer.cs`

**Purpose**: Advertises a build agent's availability on the local network using mDNS.

#### Key Features
- **mDNS service advertising**: Uses `_seudoci._tcp` service type
- **Unique service naming**: Combines "SeudoCI" prefix with agent name
- **Port configuration**: Advertises the HTTP server port for client connections
- **Automatic cleanup**: Implements IDisposable for proper resource management

#### Constructor Parameters
- `string serverName` - Agent name (spaces converted to hyphens)
- `ushort port` - HTTP server port for build requests

#### Service Registration
```csharp
var service = new ServiceProfile(serviceName, "_seudoci._tcp", port);
_serviceDiscovery.Advertise(service);
```

#### Lifecycle Management
- `Start()` - Begins advertising the service on the network
- `Stop()` - Stops advertising and removes service registration
- `Dispose()` - Cleans up mDNS resources

#### Usage Pattern
```csharp
using var server = new AgentDiscoveryServer("brilliant-dolphin", 5511);
server.Start();
// Agent now discoverable on network
// Automatic cleanup when disposed
```

### AgentDiscoveryClient

**Location**: `AgentDiscoveryClient.cs`

**Purpose**: Discovers available build agents on the local network.

#### Key Features
- **Event-driven discovery**: Fires events when agents appear/disappear
- **Automatic monitoring**: Continuously monitors network for changes
- **Service filtering**: Specifically looks for `_seudoci._tcp` services
- **Resource management**: Implements IDisposable pattern

#### Event Handlers
- `ServiceInstanceDiscovered` - Fires when new agent found
- `ServiceInstanceShutdown` - Fires when agent becomes unavailable

#### Current Implementation Status
**Note**: The current implementation is minimal and logs discoveries to console. The agent collection (`_agents`) is commented out, suggesting this is work-in-progress.

#### Lifecycle Management
- `Start()` - Begins monitoring for agent services
- `Stop()` - Stops monitoring and unregisters event handlers
- `Dispose()` - Cleans up mDNS resources and stops monitoring

#### Expected Usage Pattern
```csharp
using var client = new AgentDiscoveryClient();
client.ServiceInstanceDiscovered += (s, e) => {
    // Handle discovered agent
    Console.WriteLine($"Found agent: {e.ServiceInstanceName}");
};
client.Start();
// Monitor for agents...
```

## mDNS Protocol Integration

### Service Type
- **Service name**: `_seudoci._tcp`
- **Protocol**: TCP over mDNS/Bonjour
- **Port**: Configurable (default 5511)

### Service Naming Convention
- **Format**: `SeudoCI-{agent-name}`
- **Example**: `SeudoCI-brilliant-dolphin`
- **Normalization**: Spaces replaced with hyphens

### Network Requirements
- **mDNS support**: Requires Bonjour (Windows/macOS) or Avahi (Linux)
- **Multicast networking**: UDP multicast must be enabled on network
- **Firewall configuration**: mDNS port 5353 and agent HTTP ports must be accessible

## Dependencies

### External Packages
- **Makaretu.Dns.Multicast.New 0.31.0**: mDNS/Bonjour implementation
- **Newtonsoft.Json 13.0.3**: JSON serialization for agent data

### Internal Dependencies
- **SeudoCI.Net.Shared**: Contains shared interfaces and discovery beacon contracts

## Integration with SeudoCI System

### Agent Registration Workflow
1. Build agent starts HTTP server on configured port
2. `AgentDiscoveryServer` advertises service via mDNS
3. Service becomes discoverable to other agents and clients
4. Agent processes build requests via REST API

### Agent Discovery Workflow
1. Client creates `AgentDiscoveryClient` instance
2. Client starts monitoring for `_seudoci._tcp` services
3. Available agents discovered through mDNS events
4. Client selects agent and submits builds via HTTP

### Unity Integration
The SeudoCI.Client.Unity project uses this library for agent discovery in the AgentBrowser window, enabling Unity developers to see available build agents in real-time.

## Platform Support

### Windows
- **Bonjour for Windows**: Requires Apple's Bonjour service
- **Built-in support**: Windows 10+ has native mDNS support
- **Firewall**: Windows Defender must allow mDNS traffic

### macOS
- **Native Bonjour**: Built-in mDNS support
- **No additional setup**: Works out of the box
- **Network isolation**: Respects network boundaries

### Linux
- **Avahi daemon**: Requires avahi-daemon for mDNS
- **Package installation**: `avahi-daemon` and `avahi-utils`
- **Service configuration**: May require manual service startup

## Development Considerations

### Current Limitations
1. **Incomplete client implementation**: Agent collection and management not fully implemented
2. **Limited error handling**: Network failures not gracefully handled
3. **No agent validation**: Discovered services not verified as valid SeudoCI agents
4. **Missing reconnection logic**: No automatic retry for failed discoveries

### Planned Enhancements
Based on the current implementation state, likely planned features include:
1. **Complete agent management**: Full collection and tracking of discovered agents
2. **Agent validation**: HTTP health checks to verify agent availability
3. **Connection monitoring**: Automatic detection of agent disconnections
4. **Load balancing**: Intelligent agent selection for build distribution

### Testing Strategy
- **Local network testing**: Verify mDNS discovery on isolated networks
- **Multi-platform testing**: Ensure compatibility across Windows, macOS, and Linux
- **Network simulation**: Test behavior with network partitions and failures
- **Performance testing**: Validate discovery speed with multiple agents

## Security Considerations

### Network Security
- **Local network only**: mDNS operates within local broadcast domain
- **No authentication**: Current implementation has no agent verification
- **Plain text communication**: Service names and addresses transmitted unencrypted

### Recommended Security Measures
1. **Network isolation**: Use dedicated build networks when possible
2. **Agent validation**: Implement HTTP-based agent verification
3. **Access control**: Restrict mDNS traffic to authorized networks
4. **Monitoring**: Log agent discovery events for security auditing