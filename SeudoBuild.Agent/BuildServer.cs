using System;
using System.Threading;
using Nancy;
using Nancy.Hosting.Self;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    public class BuildServer
    {
        string agentName;
        int port;
        ModuleLoader modules;

        public BuildServer(string agentName, int port, ModuleLoader modules)
        {
            this.agentName = agentName;
            this.port = port;
        }

        public void Start()
        {
            var serverInfo = new ServerInfo();
            var discovery = new UdpDiscoveryServer(serverInfo);
            discovery.Start();

            var uri = new Uri($"http://localhost:{port}");
            using (var host = new NancyHost(uri))
            {
                StaticConfiguration.DisableErrorTraces = false;

                Console.WriteLine("Starting build server: " + uri);
                Console.WriteLine();

                host.Start();
                Console.ReadKey();
            }
        }
    }
}
