using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.IO;

namespace SeudoBuild.Net
{
    /// <summary>
    /// Watches for build agents on the local network.
    /// </summary>
    public class AgentLocator
    {
        private int _port;
        private UdpDiscoveryClient _client;
        private readonly Dictionary<Guid, AgentLocation> _agents = new Dictionary<Guid, AgentLocation>();

        /// <summary>
        /// Occurs when an agent is found on the local network.
        /// </summary>
        public event Action<AgentLocation> AgentFound;

        /// <summary>
        /// Occurs when a known agent is no longer seen on the local network.
        /// </summary>
        public event Action<AgentLocation> AgentLost;

        /// <summary>
        /// All agents that are currently visible on the local network.
        /// </summary>
        public IEnumerable<AgentLocation> Agents => _agents.Values;

        /// <summary>
        /// Watches for build agents that broadcast their availability on the given UDP port.
        /// After construction, call Start() to begin listening for agents.
        /// </summary>
        public AgentLocator(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Begin watching for build agents on the local network.
        /// </summary>
        public void Start()
        {
            if (_client == null)
            {
                _client = new UdpDiscoveryClient();
            }

            if (!_client.IsRunning)
            {
                try
                {
                    _client.Start();
                    _client.ServerFound += OnServerFound;
                    _client.ServerLost += OnServerLost;
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
            if (_client.IsRunning)
            {
                _client.Stop();
                _client.ServerFound -= OnServerFound;
                _client.ServerLost -= OnServerLost;
            }

            _client.Dispose();
            _client = null;
        }

        /// <summary>
        /// Occurs when a UdpDiscoveryServer is first seen.
        /// </summary>
        private void OnServerFound(UdpDiscoveryBeacon beacon)
        {
            // Ignore agents that we already know about
            if (_agents.ContainsKey(beacon.Guid))
            {
                return;
            }

            _agents[beacon.Guid] = null;
            RequestAgentInfoAsync(beacon);
        }

        /// <summary>
        /// Requests additional information about a build agent's capabilities
        /// after its discovery beacon is first received.
        /// </summary>
        private async Task RequestAgentInfoAsync(UdpDiscoveryBeacon beacon)
        {
            var address = $"http://{beacon.Address}:{beacon.Port}/info";

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
                var readStream = new StreamReader(resultStream, System.Text.Encoding.UTF8);
                var json = readStream.ReadToEnd();

                var agent = Newtonsoft.Json.JsonConvert.DeserializeObject<AgentLocation>(json);
                // FIXME should the agent have set its own address before responding? Does it even know its own address?
                agent.Address = beacon.Address.ToString();
                //logger.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");

                _agents[beacon.Guid] = agent;
                AgentFound?.Invoke(agent);
            }
            else
            {
                Console.WriteLine("error");
                throw new Exception("Agent info could not be obtained: " + requestTask.Exception.Message);
            }
        }

        /// <summary>
        /// Occurs when a known UdpDiscoveryServer has not been seen for some time.
        /// </summary>
        private void OnServerLost(UdpDiscoveryBeacon server)
        {
            var haveAgent = _agents.TryGetValue(server.Guid, out var agent);
            haveAgent = haveAgent && agent != null;

            if (haveAgent)
            {
                _agents.Remove(server.Guid);
                AgentLost?.Invoke(agent);
            }
        }
    }
}
