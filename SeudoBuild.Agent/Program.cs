using System;
using CommandLine;
using System.IO;
using Nancy.Hosting.Self;
using SeudoBuild.Pipeline;
using SeudoBuild.Net;
using System.Net;
using System.Threading;

namespace SeudoBuild.Agent
{
    class Program
    {
        [Verb("build", HelpText = "Create a local build.")]
        class BuildSubOptions
        {
            [Option('t', "build-target", HelpText = "Name of the build target as specified in the project configuration file. If no build target is specified, the first target will be used.")]
            public string BuildTarget { get; set; }

            [Option('o', "output-folder", HelpText = "Path to the build output folder.")]
            public string OutputPath { get; set; }

            [Value(0, MetaName = "project", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }
        }

        [Verb("scan", HelpText = "List build agents found on the local network.")]
        class ScanSubOptions
        {
        }

        [Verb("submit", HelpText = "Submit a build request for a remote build agent to fulfill.")]
        class SubmitSubOptions
        {
            [Option('p', "project-config", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }

            [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.")]
            public string BuildTarget { get; set; }

            [Option('a', "agent-name", HelpText = "The unique name of a specific build agent. If not set, the job will be broadcast to all available agents.")]
            public string AgentName { get; set; }
        }

        [Verb("queue", HelpText = "Queue build requests received over the network.")]
        class QueueSubOptions
        {
            [Option('n', "agent-name", HelpText = "A unique name for the build agent. If not set, a name will be generated.")]
            public string AgentName { get; set; }

            [Option('p', "port", HelpText = "Port on which to listen for build queue messages.")]
            public int? Port { get; set; }
        }

        [Verb("deploy", HelpText = "Listen for deployment messages.")]
        class DeploySubOptions
        {
        }

        [Verb("name", Hidden = true)]
        class NameSubOptions
        {
            [Option('r', "random")]
            public bool Random { get; set; }
        }

        public static void Main(string[] args)
        {
            Console.Title = "SeudoBuild";

            Parser.Default.ParseArguments<BuildSubOptions, ScanSubOptions, SubmitSubOptions, QueueSubOptions, DeploySubOptions, NameSubOptions>(args)
                .MapResult(
                    (BuildSubOptions opts) => Build(opts),
                    (ScanSubOptions opts) => Scan(opts),
                    (SubmitSubOptions opts) => Submit(opts),
                    (QueueSubOptions opts) => Queue(opts),
                    (DeploySubOptions opts) => Deploy(opts),
                    (NameSubOptions opts) => ShowAgentName(opts),
                    errs => 1
                );
        }

        /// <summary>
        /// Build a single target, then exit.
        /// </summary>
        static int Build(BuildSubOptions opts)
        {
            Console.Title = "SeudoBuild • Build";

            // Load pipeline modules
            var factory = new ModuleLoaderFactory();
            ModuleLoader modules = factory.Create();

            // Load project config
            ProjectConfig projectConfig = null;
            try
            {
                var fs = new FileSystem();
                var serializer = new Serializer(fs);
                var converters = modules.Registry.GetJsonConverters();
                projectConfig = serializer.DeserializeFromFile<ProjectConfig>(opts.ProjectConfigPath, converters);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't parse project config:");
                Console.WriteLine(e.Message);
                return 1;
            }

            // Execute build
            Builder builder = new Builder();

            string parentDirectory = opts.OutputPath;
            if (string.IsNullOrEmpty(parentDirectory))
            {
                // Config file's directory
                parentDirectory = new FileInfo(opts.ProjectConfigPath).Directory.FullName;
            }

            bool success = builder.Build(projectConfig, opts.BuildTarget, parentDirectory, modules);

            return success ? 0 : 1;
        }

        /// <summary>
        /// Discover build agents on the network.
        /// </summary>
        static int Scan(ScanSubOptions opts)
        {
            Console.WriteLine("Looking for build agents. Press any key to exit.");
            // FIXME fill in port from command line argument
            UdpDiscoveryClient client = new UdpDiscoveryClient(5511);
            try
            {
                client.Start();
            }
            catch (System.Net.Sockets.SocketException)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not start build agent discovery client");
                Console.ResetColor();
                return 1;
            }
            client.ServerFound += (beacon) =>
            {
                string address = $"http://{beacon.address}:{beacon.port}/info";
                using (var webClient = new WebClient())
                {
                    string json = webClient.DownloadString(address);
                    var agentInfo = Newtonsoft.Json.JsonConvert.DeserializeObject<Agent>(json);
                    BuildConsole.WriteBullet($"{agentInfo.AgentName} ({beacon.address.ToString()})");
                }
                //var request = WebRequest.Create(address);
                //var response = request.GetResponse();
            };
            Console.WriteLine();
            Console.ReadKey();
            return 0;
        }

        /// <summary>
        /// Submit a build job to another agent.
        /// </summary>
        static int Submit(SubmitSubOptions opts)
        {
            Console.Title = "SeudoBuild • Submit";
            string configJson = null;
            try
            {
                configJson = File.ReadAllText(opts.ProjectConfigPath);
            }
            catch
            {
                BuildConsole.WriteFailure("Project could not be read from " + opts.ProjectConfigPath);
                return 1;
            }

            var submit = new BuildSubmitter();
            try
            {
                submit.Submit(configJson, opts.BuildTarget, opts.AgentName);
            }
            catch (Exception e)
            {
                BuildConsole.WriteFailure("Could not submit job: " + e.Message);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Receive build jobs from other agents or clients, queue them, and execute them.
        /// Continue listening until user exits.
        /// </summary>
        static int Queue(QueueSubOptions opts)
        {
            Console.Title = "SeudoBuild • Queue";

            //string agentName = string.IsNullOrEmpty(opts.AgentName) ? AgentName.GetUniqueAgentName() : opts.AgentName;
            // FIXME pull port from command line argument, and incorporate into ServerBeacon object
            int port = 5511;
            if (opts.Port.HasValue)
            {
                port = opts.Port.Value;
            }

            // Starting the Nancy server will automatically execute the Bootstrapper class
            var uri = new Uri($"http://localhost:{port}");
            using (var host = new NancyHost(uri))
            {
                Console.WriteLine("Starting build server: " + uri);
                try
                {
                    host.Start();
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not start build server: " + e.Message);
                    Console.ResetColor();
                    return 1;
                }
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            return 0;
        }

        /// <summary>
        /// Deploy a build product on the local machine.
        /// </summary>
        static int Deploy(DeploySubOptions opts)
        {
            return 0;
        }

        /// <summary>
        /// Display the unique name for this agent.
        /// </summary>
        static int ShowAgentName(NameSubOptions opts)
        {
            string name = null;
            if (opts.Random)
            {
                name = AgentName.GetRandomName();
            }
            else
            {
                name = AgentName.GetUniqueAgentName();
            }
            Console.WriteLine();
            Console.WriteLine(name);
            Console.WriteLine();
            return 0;
        }
    }
}
