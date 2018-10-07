using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using SeudoBuild.Net;
using System.IO;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Submit a build process to an agent on the local network.
    /// </summary>
    public class BuildSubmitter
    {
        // FIXME don't hard-code
        private const int Port = 5511;

        private readonly ILogger _logger;

        public BuildSubmitter(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Submit the given project configuration and build target to a build
        /// agent on the network.
        /// </summary>
        public void Submit(string projectJson, string target, string agentName)
        {
            _logger.Write("Submitting build to " + agentName);

            // Find agent on the network, with timeout
            var discovery = new UdpDiscoveryClient();
            discovery.Start();

            //while (!discovery.AvailableServers.Any (s => s.guid);

            var client = new UdpDiscoveryClient();
            try
            {
                client.Start();
            }
            catch (System.Net.Sockets.SocketException)
            {
                throw new Exception("Could not start build agent discovery client");
            }

            client.ServerFound += (beacon) =>
            {
                // Validate build agent name
                var agentAddress = $"http://{beacon.Address}:{beacon.Port}/info";
                AgentLocation agentInfo;
                using (var webClient = new WebClient())
                {
                    var json = webClient.DownloadString(agentAddress);
                    agentInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<AgentLocation>(json);
                }
                if (agentInfo.AgentName != agentName)
                {
                    return;
                }

                // Send build request

                var command = target != null ? $"submit/target" : "submit";
                var address = $"http://{beacon.Address}:{beacon.Port}/{command}";

                var request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = projectJson.Length;
                var requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
                requestWriter.Write(projectJson);
                requestWriter.Close();

                try
                {
                    var webResponse = request.GetResponse();
                    var webStream = webResponse.GetResponseStream();
                    var responseReader = new StreamReader(webStream);
                    var response = responseReader.ReadToEnd();

                    // TODO handle the response
                    Console.Out.WriteLine(response);

                    responseReader.Close();
                }
                catch (Exception)
                {
                    // TODO handle exception
                }
            };
        }
    }
}
