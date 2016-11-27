using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using SeudoBuild.Net;
using System.Collections.Generic;
using SeudoBuild.Pipeline;
using System.Linq;

namespace SeudoBuild.Client.Unity
{
    public class AgentBrowser
    {
        UdpDiscoveryClient client;

        class AgentInfo
        {
            public System.Guid guid;
            public string name;
            public string address;
        }

        List<AgentInfo> agents = new List<AgentInfo>();

        public void Start()
        {
            if (client == null)
            {
                client = new UdpDiscoveryClient();
            }
            if (!client.IsRunning)
            {
                client.Start();
                client.ServerFound += OnServerFound;
                client.ServerFound += OnServerLost;
            }
        }

        public void Stop()
        {
            if (!client.IsRunning)
            {
                return;
            }
            client.Stop();
            client.ServerFound -= OnServerFound;
            client.ServerFound -= OnServerLost;
        }

        void OnServerFound(ServerBeacon server)
        {
            if (!agents.Any(a => a.guid == server.guid))
            {
                var agent = new AgentInfo { guid = server.guid, address = server.address.ToString() };
                // TODO async http request to get the agent's name
                agents.Add(agent);
            }
        }

        void OnServerLost(ServerBeacon server)
        {
            var agent = agents.FirstOrDefault(a => a.guid == server.guid);
            if (agent != null)
            {
                agents.Remove(agent);
            }
        }

        public void Draw()
        {
            foreach (var agent in agents)
            {
                GUILayout.Label(agent.name);
            }
        }
    }
}
