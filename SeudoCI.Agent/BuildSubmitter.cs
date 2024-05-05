namespace SeudoCI.Agent;

using System.Net;
using Net;
using Core;

/// <summary>
/// Submit a build process to an agent on the local network.
/// </summary>
public class BuildSubmitter(ILogger logger)
{
    // FIXME don't hard-code
    //private const int Port = 5511;

    /// <summary>
    /// Submit the given project configuration and build target to a build
    /// agent on the network.
    /// </summary>
    public void Submit(IDiscoveryClient<IDiscoveryBeacon> discoveryClient, string projectJson, string target, string agentName)
    {
        logger.Write("Submitting build to " + agentName);
            
        try
        {
            discoveryClient.Start();
        }
        catch (System.Net.Sockets.SocketException)
        {
            throw new Exception("Could not start build agent discovery client");
        }

        discoveryClient.ServerFound += beacon =>
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

            var command = target != null ? "submit/target" : "submit";
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