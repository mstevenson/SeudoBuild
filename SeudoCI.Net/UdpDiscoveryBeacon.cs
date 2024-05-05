using System;
using System.Net;

namespace SeudoCI.Net
{
    /// <summary>
    /// A beacon packet that is broadcast at regular intervals by a
    /// UdpDiscoveryServer. A UdpDiscoveryClient listens for beacons, enabling
    /// automatic service discovery on the local network.
    /// </summary>
    public class UdpDiscoveryBeacon : IDiscoveryBeacon
    {
        /// <summary>
        /// String identifier that begins each UDP server discovery message.
        /// </summary>
        public const string ServiceId = "SEUD";

        public ushort Version { get; set; } = 1;

        public IPAddress Address { get; set; }

        /// <summary>
        /// Unique ID for a server.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Port for REST API.
        /// </summary>
        public ushort Port { get; set; }

        public DateTime LastSeen { get; set; }

        public UdpDiscoveryBeacon()
        {
            Guid = Guid.NewGuid();
            LastSeen = DateTime.Now;
            Address = IPAddress.Parse("127.0.0.1");
        }

        public static UdpDiscoveryBeacon FromBytes(byte[] data)
        {
            var index = 0;

            // ASCII Service ID
            var encoding = new System.Text.ASCIIEncoding();
            byte[] serviceIdBytes = encoding.GetBytes(ServiceId);
            var id = encoding.GetString(data, 0, serviceIdBytes.Length);
            index += id.Length;

            // Bail out if the beacon was not the service we're looking for
            if (id != ServiceId)
            {
                return null;
            }

            // Beacon version number
            byte[] versionBytes = Slice(data, index, 2);
            var version = BitConverter.ToUInt16(versionBytes, 0);
            index += versionBytes.Length;

            // Server GUID
            byte[] guidBytes = Slice(data, index, 16);
            var guid = new Guid(guidBytes);
            index += guidBytes.Length;

            // REST API port
            byte[] portBytes = Slice(data, index, 2);
            var port = BitConverter.ToUInt16(portBytes, 0);
            index += portBytes.Length;

            return new UdpDiscoveryBeacon { Port = port, Version = version, Guid = guid };
        }

        private static byte[] Slice(byte[] source, int index, int length)
        {
            var output = new byte[length];
            Array.Copy(source, index, output, 0, length);
            return output;
        }

        public static byte[] ToBytes(UdpDiscoveryBeacon config)
        {
            var output = new byte[24];
            int offset = 0;

            // 4 byte ASCII service ID
            var encoding = new System.Text.ASCIIEncoding();
            byte[] serviceIdBytes = encoding.GetBytes(ServiceId);
            offset = AppendBytes(serviceIdBytes, output, offset);

            // 2 byte version number
            byte[] versionBytes = BitConverter.GetBytes(config.Version);
            offset = AppendBytes(versionBytes, output, offset);

            // 16 byte server GUID
            byte[] guidBytes = config.Guid.ToByteArray();
            offset = AppendBytes(guidBytes, output, offset);

            // 2 byte REST API port
            byte[] sendReceivePortBytes = BitConverter.GetBytes(config.Port);
            offset = AppendBytes(sendReceivePortBytes, output, offset);

            return output;
        }

        private static int AppendBytes(byte[] source, byte[] target, int offset)
        {
            Buffer.BlockCopy(source, 0, target, offset, source.Length);
            return offset + source.Length;
        }

        public override string ToString()
        {
            //return string.Format ("{0} v{1} {2} {3} {4}", serviceId, version, address, sendReceivePort, broadcastPort);
            return $"{ServiceId} v{Version} {Address}";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Address.GetHashCode();
                //hash = hash * 23 + sendReceivePort.GetHashCode ();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            var s = obj as UdpDiscoveryBeacon;
            if (s == null)
            {
                return false;
            }

            return Guid == s.Guid;
        }

        public bool Equals(UdpDiscoveryBeacon s)
        {
            if ((object) s == null)
            {
                return false;
            }

            return Guid == s.Guid;
        }

        public static bool operator ==(UdpDiscoveryBeacon a, UdpDiscoveryBeacon b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object) a == null || (object) b == null)
            {
                return false;
            }

            return a.Guid == b.Guid;
        }

        public static bool operator !=(UdpDiscoveryBeacon a, UdpDiscoveryBeacon b)
        {
            return !(a == b);
        }
    }
}

