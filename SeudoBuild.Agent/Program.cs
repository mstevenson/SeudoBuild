using System;
using CommandLine;
using System.IO;
using Nancy.Hosting.Self;
using SeudoBuild.Core;
using SeudoBuild.Core.FileSystems;
using SeudoBuild.Pipeline;
using SeudoBuild.Net;

namespace SeudoBuild.Agent
{
    class Program
    {
        private const string Header = @"
                _     _       _ _   _ 
  ___ ___ _ _ _| |___| |_ _ _|_| |_| |
 |_ -| -_| | | . | . | . | | | | | . |
 |___|___|___|___|___|___|___|_|_|___|
                                      
";

        private static ILogger _logger;

        [Verb("build", HelpText = "Create a local build.")]
        private class BuildSubOptions
        {
            [Option('t', "build-target", HelpText = "Name of the build target as specified in the project configuration file. If no build target is specified, the first target will be used.")]
            public string BuildTarget { get; set; }

            [Option('o', "output-folder", HelpText = "Path to the build output folder.")]
            public string OutputPath { get; set; }

            [Value(0, MetaName = "project", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }
        }

        [Verb("scan", HelpText = "List build agents found on the local network.")]
        private class ScanSubOptions
        {
        }

        [Verb("submit", HelpText = "Submit a build request for a remote build agent to fulfill.")]
        private class SubmitSubOptions
        {
            [Option('p', "project-config", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }

            [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.")]
            public string BuildTarget { get; set; }

            [Option('a', "agent-name", HelpText = "The unique name of a specific build agent. If not set, the job will be broadcast to all available agents.")]
            public string AgentName { get; set; }
        }

        [Verb("queue", HelpText = "Queue build requests received over the network.")]
        private class QueueSubOptions
        {
            [Option('n', "agent-name", HelpText = "A unique name for the build agent. If not set, a name will be generated.")]
            public string AgentName { get; set; }

            [Option('p', "port", HelpText = "Port on which to listen for build queue messages.")]
            public int? Port { get; set; }
        }

        [Verb("deploy", HelpText = "Listen for deployment messages.")]
        private class DeploySubOptions
        {
        }

        [Verb("name", Hidden = true)]
        private class NameSubOptions
        {
            [Option('r', "random")]
            public bool Random { get; set; }
        }

        public static void Main(string[] args)
        {
            _logger = new Logger();

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
        private static int Build(BuildSubOptions opts)
        {
            Console.Title = "SeudoBuild • Build";
            Console.WriteLine(Header);

            // Load pipeline modules
            var factory = new ModuleLoaderFactory();
            IModuleLoader moduleLoader = factory.Create(_logger);

            // Load project config
            ProjectConfig projectConfig = null;
            try
            {
                var fs = new WindowsFileSystem();
                var serializer = new Serializer(fs);
                var converters = moduleLoader.Registry.GetJsonConverters();
                projectConfig = serializer.DeserializeFromFile<ProjectConfig>(opts.ProjectConfigPath, converters);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't parse project config:");
                Console.WriteLine(e.Message);
                return 1;
            }

            // Execute build
            var builder = new Builder(moduleLoader, _logger);

            var parentDirectory = opts.OutputPath;
            if (string.IsNullOrEmpty(parentDirectory))
            {
                // Config file's directory
                parentDirectory = new FileInfo(opts.ProjectConfigPath).Directory?.FullName;
            }

            var pipeline = new PipelineRunner(new PipelineConfig { BaseDirectory = parentDirectory }, _logger);
            bool success = builder.Build(pipeline, projectConfig, opts.BuildTarget);

            return success ? 0 : 1;
        }

        /// <summary>
        /// Discover build agents on the network.
        /// </summary>
        private static int Scan(ScanSubOptions opts)
        {
            Console.Title = "SeudoBuild • Scan";
            Console.WriteLine(Header);

            Console.WriteLine("Looking for build agents. Press any key to exit.");
            // FIXME fill in port from command line argument
            var locator = new AgentLocator(5511);
            try
            {
                locator.Start();
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Could not start build agent discovery client");
                Console.ResetColor();
                return 1;
            }
            // FIXME don't hard-code port
            locator.AgentFound += (agent) =>
            {
                _logger.Write($"{agent.AgentName} ({agent.Address})", LogType.Bullet);
            };
            locator.AgentLost += (agent) =>
            {
                _logger.Write($"Lost agent: {agent.AgentName} ({agent.Address})", LogType.Bullet);
            };
            Console.WriteLine();
            Console.ReadKey();
            return 0;
        }

        /// <summary>
        /// Submit a build job to another agent.
        /// </summary>
        private static int Submit(SubmitSubOptions opts)
        {
            Console.Title = "SeudoBuild • Submit";
            Console.WriteLine(Header);

            string configJson = null;
            try
            {
                configJson = File.ReadAllText(opts.ProjectConfigPath);
            }
            catch
            {
                _logger.Write("Project could not be read from " + opts.ProjectConfigPath, LogType.Failure);
                return 1;
            }

            var buildSubmitter = new BuildSubmitter(_logger);
            try
            {
                // Find agent on the network, with timeout
                var discoveryClient = new UdpDiscoveryClient();
                buildSubmitter.Submit(discoveryClient, configJson, opts.BuildTarget, opts.AgentName);
            }
            catch (Exception e)
            {
                _logger.Write("Could not submit job: " + e.Message, LogType.Failure);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Receive build jobs from other agents or clients, queue them, and execute them.
        /// Continue listening until user exits.
        /// </summary>
        private static int Queue(QueueSubOptions opts)
        {
            Console.Title = "SeudoBuild • Queue";
            Console.WriteLine(Header);

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
                _logger.Write("");
                try
                {
                    host.Start();

                    _logger.Write("Build Queue", LogType.Header);
                    _logger.Write("");
                    _logger.Write("Started build agent server: " + uri, LogType.Bullet);

                    try
                    {
                        // FIXME configure the port from a command line argument
                        var serverInfo = new UdpDiscoveryBeacon { Port = 5511 };
                        var discovery = new UdpDiscoveryServer(serverInfo);
                        discovery.Start();
                        _logger.Write("Build agent discovery beacon started", LogType.Bullet);
                    }
                    catch
                    {
                        _logger.Write("Could not initialize build agent discovery beacon", LogType.Alert);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Could not start build server: " + e.Message);
                    Console.ResetColor();
                    return 1;
                }
                Console.WriteLine("");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
            }

            return 0;
        }

        /// <summary>
        /// Deploy a build product on the local machine.
        /// </summary>
        private static int Deploy(DeploySubOptions opts)
        {
            return 0;
        }

        /// <summary>
        /// Display the unique name for this agent.
        /// </summary>
        private static int ShowAgentName(NameSubOptions opts)
        {
            string name;
            name = opts.Random ? AgentName.GetRandomName() : AgentName.GetUniqueAgentName();
            Console.WriteLine();
            Console.WriteLine(name);
            Console.WriteLine();
            return 0;
        }
    }
}
