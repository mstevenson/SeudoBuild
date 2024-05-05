// using UnityEngine;
// using UnityEditor;
// using UnityEngine.Networking;
// using SeudoBuild.Net;
// using System.Collections.Generic;
// using SeudoBuild.Pipeline;
//
// namespace SeudoBuild.Client.Unity
// {
//     public class AgentBrowser : EditorWindow
//     {
//         AgentLocator locator;
//
//         List<AgentLocation> agents = new List<AgentLocation>();
//
//         [MenuItem("Window/Build Agent Browser")]
//         static void Init()
//         {
//             var window = CreateInstance<AgentBrowser>();
//             window.Show();
//         }
//
//         void OnEnable()
//         {
//             // FIXME configure port
//             locator = new AgentLocator(5511);
//
//             locator.Start();
//             Debug.Log("start locator");
//             locator.AgentFound += HandleAgentFound;
//             locator.AgentLost += HandleAgentLost;
//
//
//             //if (client == null)
//             //{
//             //    client = new UdpDiscoveryClient(5511);
//             //}
//             //if (!client.IsRunning)
//             //{
//             //    client.Start();
//             //    client.ServerFound += OnServerFound;
//             //    client.ServerFound += OnServerLost;
//             //}
//         }
//
//         void HandleAgentFound(AgentLocation agent)
//         {
//             if (!agents.Contains(agent))
//             {
//                 agents.Add(agent);
//             }
//         }
//
//         void HandleAgentLost(AgentLocation agent)
//         {
//             if (agents.Contains(agent))
//             {
//                 agents.Remove(agent);
//             }
//         }
//
//         void OnDisable()
//         {
//             if (locator != null)
//             {
//                 Debug.Log("stop locator");
//                 locator.AgentFound -= HandleAgentFound;
//                 locator.AgentLost -= HandleAgentLost;
//                 locator.Stop();
//             }
//
//
//             //if (!client.IsRunning)
//             //{
//             //    return;
//             //}
//             //client.Stop();
//             //client.ServerFound -= OnServerFound;
//             //client.ServerFound -= OnServerLost;
//         }
//
//
//         void OnGUI()
//         {
//             foreach (var agent in locator.Agents)
//             {
//                 GUILayout.Label(agent.AgentName);
//                 GUILayout.Label(agent.Address);
//                 GUILayout.Space(10);
//             }
//         }
//     }
// }
