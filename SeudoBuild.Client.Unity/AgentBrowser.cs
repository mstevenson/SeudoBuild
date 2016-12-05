using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using SeudoBuild.Net;
using System.Collections.Generic;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Client.Unity
{
    public class AgentBrowser
    {
        //AgentLocator locator;

        //List<AgentInfo> agents = new List<AgentInfo>();

        public void Start()
        {
            // FIXME configure port
            //locator = new AgentLocator(5511);

            //if (client == null)
            //{
            //    client = new UdpDiscoveryClient(5511);
            //}
            //if (!client.IsRunning)
            //{
            //    client.Start();
            //    client.ServerFound += OnServerFound;
            //    client.ServerFound += OnServerLost;
            //}
        }

        public void Stop()
        {
            //locator.Stop();

            //if (!client.IsRunning)
            //{
            //    return;
            //}
            //client.Stop();
            //client.ServerFound -= OnServerFound;
            //client.ServerFound -= OnServerLost;
        }


        public void Draw()
        {
            //foreach (var agent in locator.Agents)
            //{
            //    GUILayout.Label(agent.name);
            //}
        }
    }
}
