using System;
using System.Collections.Generic;
using System.Net;
using SeudoBuild.Net;
using System.Threading.Tasks;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Listens for build agents on the local network.
    /// </summary>
    public class AgentLocator
    {
        int port;
        UdpDiscoveryClient client;
        Dictionary<Guid, Agent> agents = new Dictionary<Guid, Agent>();

        /// <summary>
        /// Occurs when an agent is found on the local network.
        /// </summary>
        public event Action<Agent> AgentFound = delegate { };

        /// <summary>
        /// Occurs when a known agent is no longer seen on the local network.
        /// </summary>
        public event Action<Agent> AgentLost = delegate { };

        /// <summary>
        /// All agents that are currently visible on the local network.
        /// </summary>
        public IEnumerable<Agent> Agents
        {
            get
            {
                return agents.Values;
            }
        }

        /// <summary>
        /// Watches for build agents that broadcast their availability on the given UDP port.
        /// After construction, call Start() to begin listening for agents.
        /// </summary>
        public AgentLocator(int port)
        {
            this.port = port;
        }

        /// <summary>
        /// Begin watching for build agents on the local network.
        /// </summary>
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

        /// <summary>
        /// Stop watching for build agents on the local network.
        /// </summary>
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

        void OnServerFound(UdpDiscoveryResponse discoveryResponse)
        {
            // Ignore agents that we already know about
            if (!agents.ContainsKey (discoveryResponse.guid))
            {
                RequestAgentInfoAsync(discoveryResponse);
            }
        }

        async Task RequestAgentInfoAsync(UdpDiscoveryResponse discoveryResponse)
        {
            string address = $"http://{discoveryResponse.address}:{discoveryResponse.port}/info";
            using (var webClient = new WebClient())
            {
                // Request the agent's identity
                Task<string> requestTask = webClient.DownloadStringTaskAsync(address);
                await requestTask;
                if (requestTask.IsCompleted)
                {
                    string json = requestTask.Result;
                    var agent = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
                    //BuildConsole.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");

                    agents.Add(discoveryResponse.guid, agent);
                    AgentFound.Invoke(agent);
                }
                else {
                    throw new Exception("Agent info could not be obtained: " + requestTask.Exception.Message);
                }
            }
        }

        void OnServerLost(UdpDiscoveryResponse server)
        {
            Agent agent;
            if (agents.TryGetValue (server.guid, out agent)) {
                agents.Remove(server.guid);
                AgentLost.Invoke(agent);
            }
        }
    }
}
