using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using SeudoBuild.Net;
using System.IO;

namespace SeudoBuild.Agent
{
    public class BuildSubmitter
    {
        // FIXME don't hard-code
        const int port = 5511;

        public void Submit(string projectJson, string target, string agentName)
        {
            Console.WriteLine("Submitting build to " + agentName);

            // Find agent on the network, with timeout
            var discovery = new UdpDiscoveryClient(port);
            discovery.Start();

            //while (!discovery.AvailableServers.Any (s => s.guid);

            UdpDiscoveryClient client = new UdpDiscoveryClient(port);
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
                string agentAddress = $"http://{beacon.address}:{beacon.port}/info";
                Agent agentInfo = null;
                using (var webClient = new WebClient())
                {
                    string json = webClient.DownloadString(agentAddress);
                    agentInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
                }
                if (agentInfo.AgentName != agentName)
                {
                    return;
                }

                // Send build request

                string command = target != null ? $"submit/target" : "submit";
                string address = $"http://{beacon.address}:{beacon.port}/{command}";

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = projectJson.Length;
                StreamWriter requestWriter = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
                requestWriter.Write(projectJson);
                requestWriter.Close();

                try
                {
                    WebResponse webResponse = request.GetResponse();
                    Stream webStream = webResponse.GetResponseStream();
                    StreamReader responseReader = new StreamReader(webStream);
                    string response = responseReader.ReadToEnd();

                    // TODO handle the response
                    Console.Out.WriteLine(response);

                    responseReader.Close();
                }
                catch (Exception e)
                {
                    // TODO handle exception
                }
            };
        }
    }
}
