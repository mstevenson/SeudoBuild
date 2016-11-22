using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeudoBuild.Agent
{
    public class BuildSubmit
    {
        public void Submit(string projectConfigPath, string buildTarget, string agentName)
        {
            Console.WriteLine($"Searching for network build agent named {agentName}");

            //UdpDiscoveryClient discovery = new UdpDiscoveryClient();
            //discovery.ServerFound += (ServerInfo serverInfo) =>
            //{
            //    Console.WriteLine("Found build agent: " + serverInfo.address);
            //    //SendMessage(serverInfo);
            //};
        }

        //void SendMessage(ServerInfo serverInfo)
        //{
        //    Console.WriteLine("Submitting build job");

            //using (var client = new RequestSocket())
            //{
            //    client.Connect($"tcp://{serverInfo.address}:{serverInfo.sendReceivePort}");

            //    for (int i = 0; i < 10; i++)
            //    {
            //        Console.WriteLine("Sending Hello");
            //        bool success = client.TrySendFrame(TimeSpan.FromSeconds(4), "Hello");
            //        if (!success)
            //        {
            //            throw new Exception("Could not send message, timed out");
            //        }


            //        var message = client.ReceiveFrameString();
            //        Console.WriteLine("Received {0}", message);
            //    }
            //}
        //}
    }
}
