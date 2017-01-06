using System;
using System.Collections.Generic;
using System.Net;
using SeudoBuild.Net;
using System.Threading.Tasks;
using System.IO;

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
        public event Action<Agent> AgentFound;

        /// <summary>
        /// Occurs when a known agent is no longer seen on the local network.
        /// </summary>
        public event Action<Agent> AgentLost;

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
                agents[discoveryResponse.guid] = null;
                RequestAgentInfoAsync(discoveryResponse);
            }
        }

        async Task RequestAgentInfoAsync(UdpDiscoveryResponse discoveryResponse)
        {
            string address = $"http://{discoveryResponse.address}:{discoveryResponse.port}/info";

            // Request the agent's identity
            var req = WebRequest.CreateHttp(address);
            req.Timeout = 2000;
            var requestTask = req.GetResponseAsync();


            // FIXME this never returns in a "scan" mode agent if a remote "queue" mode agent is run
            // after an the scan agent has already begun.
            // Is this because the "queue" mode agent sends its initial UDP packet before it's ready to respond to HTTP requests?
            // Even so, why does the following request task hang when its timeout was set to 2000ms above?
            await requestTask;

            if (requestTask.IsCompleted)
            {
                var resultStream = requestTask.Result.GetResponseStream();
                StreamReader readStream = new StreamReader(resultStream, System.Text.Encoding.UTF8);
                string json = readStream.ReadToEnd();

                var agent = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
                // FIXME should the agent have set its own address before responding? Does it even know its own address?
                agent.Address = discoveryResponse.address.ToString();
                //BuildConsole.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");

                agents[discoveryResponse.guid] = agent;
                if (AgentFound != null)
                {
                    AgentFound.Invoke(agent);
                }
            }
            else {
                Console.WriteLine("error");
                throw new Exception("Agent info could not be obtained: " + requestTask.Exception.Message);
            }
        }

        void OnServerLost(UdpDiscoveryResponse server)
        {
            Agent agent;
            bool haveAgent = agents.TryGetValue(server.guid, out agent);
            haveAgent = haveAgent && agent != null;

            if (haveAgent) {
                agents.Remove(server.guid);
                if (AgentLost != null)
                {
                    AgentLost.Invoke(agent);
                }
            }
        }
    }
}
