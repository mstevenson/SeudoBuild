using System;
using System.Net;

namespace SeudoBuild.Net
{
    public interface IDiscoveryBeacon
    {
        Guid Guid { get; }
        IPAddress Address { get; set; }
        ushort Port { get; set; }
        DateTime LastSeen { get; set; }
    }
}