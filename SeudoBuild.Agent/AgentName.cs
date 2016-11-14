using System;
using System.Net.NetworkInformation;

namespace SeudoBuild.Agent
{
    public class AgentName
    {
        public AgentName()
        {
        }

        string GetMacAddresses()
        {
            string macAddresses = string.Empty;
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                macAddresses += nic.GetPhysicalAddress().ToString();
            }
            return macAddresses;
        }
    }
}
