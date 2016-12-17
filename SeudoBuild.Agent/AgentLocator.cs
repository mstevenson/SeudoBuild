using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    public class AgentLocator
    {
        int port;
        UdpDiscoveryClient client;
        List<AgentInfo> agents = new List<AgentInfo>();

        public event Action<Agent> AgentFound;
        public event Action<Agent> AgentLost;

        public IEnumerable<AgentInfo> Agents
        {
            get
            {
                return agents;
            }
        }

        public AgentLocator(int port)
        {
            this.port = port;
        }

        public void Start()
        {
            if (client == null)
            {
                client = new UdpDiscoveryClient(port);
            }
            if (!client.IsRunning)
            {
                try
                {
                    client.Start();
                    client.ServerFound += OnServerFound;
                    client.ServerLost += OnServerLost;
                }
                catch (System.Net.Sockets.SocketException)
                {
                    // TODO
                    throw;
                }
            }
        }

        public void Stop()
        {
            if (client.IsRunning)
            {
                client.Stop();
                client.ServerFound -= OnServerFound;
                client.ServerLost -= OnServerLost;
            }
            client.Dispose();
            client = null;
        }

        void OnServerFound(UdpDiscoveryResponse server)
        {
            if (!agents.Any(a => a.guid == server.guid))
            {
                var agent = new AgentInfo { guid = server.guid, address = server.address.ToString() };
                // TODO async http request to get the agent's name
                agents.Add(agent);
            }

            string address = $"http://{server.address}:{server.port}/info";
            using (var webClient = new WebClient())
            {
                string json = webClient.DownloadString(address);
                var agentInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
                //BuildConsole.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");
            }
        }

        void OnServerLost(UdpDiscoveryResponse server)
        {
        //    var agent = agents.FirstOrDefault(a => a.guid == server.guid);
        //    if (agent != null)
        //    {
        //        agents.Remove(agent);
        //    }
        }
    }
}
