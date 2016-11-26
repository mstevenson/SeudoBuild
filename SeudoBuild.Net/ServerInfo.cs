using System;
using System.Net;

namespace SeudoBuild.Net
{
    public class ServerInfo
    {
        /// <summary>
        /// String identifier that begins each UDP server discovery message.
        /// </summary>
        public static readonly string serviceId = "SEUD";

        public ushort version = 1;

        public IPAddress address;

        /// <summary>
        /// Unique ID for a server.
        /// </summary>
        public Guid guid;

        ///// <summary>
        ///// Port for sending reliable bidirectional messages.
        ///// </summary>
        //public ushort sendReceivePort = 5125;

        ///// <summary>
        ///// Port for sending and receiving unreliable broadcasts.
        ///// </summary>
        //public ushort broadcastPort = 5126;

        public DateTime lastSeen;


        public ServerInfo()
        {
            this.guid = Guid.NewGuid();
            this.lastSeen = DateTime.Now;
            this.address = IPAddress.Parse("127.0.0.1");
        }

        public static ServerInfo FromBytes(byte[] data)
        {
            var s = new ServerInfo();
            int index = 0;

            // ASCII Service ID
            var encoding = new System.Text.ASCIIEncoding();
            byte[] serviceIdBytes = encoding.GetBytes(ServerInfo.serviceId);
            var id = encoding.GetString(data, 0, serviceIdBytes.Length);
            index += id.Length;

            // Bail out if the beacon was not the service we're looking for
            if (id != ServerInfo.serviceId)
            {
                return null;
            }

            // Beacon version number
            byte[] versionBytes = Slice(data, index, 2);
            s.version = BitConverter.ToUInt16(versionBytes, 0);
            index += versionBytes.Length;

            // Server GUID
            byte[] guidBytes = Slice(data, index, 16);
            s.guid = new Guid(guidBytes);
            index += guidBytes.Length;

            //byte[] sendReceivePortBytes = Slice (data, index, 2);
            //s.sendReceivePort = BitConverter.ToUInt16 (sendReceivePortBytes, 0);
            //index += sendReceivePortBytes.Length;

            //byte[] broadcastPortBytes = Slice (data, index, 2);
            //s.broadcastPort = BitConverter.ToUInt16 (broadcastPortBytes, 0);
            //index += broadcastPortBytes.Length;

            return s;
        }

        static byte[] Slice(byte[] source, int index, int length)
        {
            byte[] output = new byte[length];
            Array.Copy(source, index, output, 0, length);
            return output;
        }

        public static byte[] ToBytes(ServerInfo config)
        {
            byte[] output = new byte[22];
            int offset = 0;

            // 4 byte ASCII service ID
            var encoding = new System.Text.ASCIIEncoding();
            byte[] serviceIdBytes = encoding.GetBytes(ServerInfo.serviceId);
            offset = AppendBytes(serviceIdBytes, output, offset);

            // 2 byte version number
            byte[] versionBytes = BitConverter.GetBytes(config.version);
            offset = AppendBytes(versionBytes, output, offset);

            // 16 byte server GUID
            byte[] guidBytes = config.guid.ToByteArray();
            offset = AppendBytes(guidBytes, output, offset);

            //// 2 byte send/receive port
            //byte[] sendReceivePortBytes = BitConverter.GetBytes (config.sendReceivePort);
            //offset = AppendBytes (sendReceivePortBytes, output, offset);

            //// 2 byte broadcast port
            //byte[] broadcastPortBytes = BitConverter.GetBytes (config.broadcastPort);
            //offset = AppendBytes (broadcastPortBytes, output, offset);

            return output;
        }

        static int AppendBytes(byte[] source, byte[] target, int offset)
        {
            Buffer.BlockCopy(source, 0, target, offset, source.Length);
            return offset + source.Length;
        }

        public override string ToString()
        {
            //return string.Format ("{0} v{1} {2} {3} {4}", serviceId, version, address, sendReceivePort, broadcastPort);
            return $"{serviceId} v{version} {address}";
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + address.GetHashCode();
                //hash = hash * 23 + sendReceivePort.GetHashCode ();
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            ServerInfo s = obj as ServerInfo;
            if ((object)s == null)
            {
                return false;
            }
            return guid == s.guid;
        }

        public bool Equals(ServerInfo s)
        {
            if ((object)s == null)
            {
                return false;
            }
            return guid == s.guid;
        }

        public static bool operator ==(ServerInfo a, ServerInfo b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            return a.guid == b.guid;
        }

        public static bool operator !=(ServerInfo a, ServerInfo b)
        {
            return !(a == b);
        }
    }
}

