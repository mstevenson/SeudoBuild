using System;
using CommandLine;
using System.IO;
using System.Linq;
using Nancy;
using Nancy.Hosting.Self;
using SeudoBuild.Net;

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

            Parser.Default.ParseArguments<BuildSubOptions, SubmitSubOptions, QueueSubOptions, DeploySubOptions, NameSubOptions>(args)
                .MapResult(
                    (BuildSubOptions opts) => Build(opts),
                    (SubmitSubOptions opts) => Submit(opts),
                    (QueueSubOptions opts) => Queue(opts),
                    (DeploySubOptions opts) => Deploy(opts),
                    (NameSubOptions opts) => ShowAgentName(opts),
                    errs => 1
                );
        }

        // Build locally
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

        // Submit job to the network
        static int Submit(SubmitSubOptions opts)
        {
            Console.Title = "SeudoBuild • Submit";

            var submit = new BuildSubmit();
            submit.Submit(opts.ProjectConfigPath, opts.BuildTarget, opts.AgentName);

            return 0;
        }

        // Listen for jobs on the network
        static int Queue(QueueSubOptions opts)
        {
            Console.Title = "SeudoBuild • Queue";

            //string agentName = string.IsNullOrEmpty(opts.AgentName) ? AgentName.GetUniqueAgentName() : opts.AgentName;
            int port = 5555;
            if (opts.Port.HasValue)
            {
                port = opts.Port.Value;
            }

            // Starting the Nancy server will automatically execute the Bootstrapper class
            var uri = new Uri($"http://localhost:{port}");
            using (var host = new NancyHost(uri))
            {
                Console.WriteLine("Starting build server: " + uri);
                Console.WriteLine("Press any key to exit.");

                try
                {
                    host.Start();
                }
                catch
                {
                    Console.WriteLine("Could not start build server, exiting.");
                    return 1;
                }
                Console.ReadKey();
            }

            return 0;
        }

        static int Deploy(DeploySubOptions opts)
        {
            return 0;
        }

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
