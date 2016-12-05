using System.Collections.Generic;
using System.Linq;
using System.Net;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    public class AgentLocator
    {
        //int port;
        //UdpDiscoveryClient client;
        //List<AgentInfo> agents = new List<AgentInfo>();

        //public IEnumerable<AgentInfo> Agents
        //{
        //    get
        //    {
        //        return agents;
        //    }
        //}

        //public AgentLocator(int port)
        //{
        //    this.port = port;
        //}

        //public void Start()
        //{
        //    if (client != null)
        //    {
        //        return;
        //    }

        //    client = new UdpDiscoveryClient(port);
        //    try
        //    {
        //        client.Start();
        //    }
        //    catch (System.Net.Sockets.SocketException)
        //    {
        //        // TODO
        //        throw;
        //    }

        //    if (!client.IsRunning)
        //    {
        //        client.Start();
        //        client.ServerFound += OnServerFound;
        //        client.ServerFound += OnServerLost;
        //    }
        //}

        //public void Stop()
        //{
        //    if (!client.IsRunning)
        //    {
        //        return;
        //    }
        //    client.Stop();
        //    client.ServerFound -= OnServerFound;
        //    client.ServerFound -= OnServerLost;
        //}

        //void OnServerFound(UdpDiscoveryResponse server)
        //{
        //    if (!agents.Any(a => a.guid == server.guid))
        //    {
        //        var agent = new AgentInfo { guid = server.guid, address = server.address.ToString() };
        //        // TODO async http request to get the agent's name
        //        agents.Add(agent);
        //    }

        //    string address = $"http://{beacon.address}:{beacon.port}/info";
        //    using (var webClient = new WebClient())
        //    {
        //        string json = webClient.DownloadString(address);
        //        var agentInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
        //        BuildConsole.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");
        //    }
        //}

        //void OnServerLost(UdpDiscoveryResponse server)
        //{
        //    var agent = agents.FirstOrDefault(a => a.guid == server.guid);
        //    if (agent != null)
        //    {
        //        agents.Remove(agent);
        //    }
        //}
    }
}
