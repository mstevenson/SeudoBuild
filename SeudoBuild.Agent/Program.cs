using System;
using CommandLine;
using CommandLine.Text;
using System.IO;
using System.Linq;

namespace SeudoBuild.Agent
{
    class MainClass
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

            [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.", Required = true)]
            public string BuildTarget { get; set; }

            [Option('a', "agent-name", HelpText = "The unique name of a specific build agent. If not set, the job will be broadcast to all available agents.")]
            public string Host { get; set; }
        }

        [Verb("queue", HelpText = "Queue build requests received over the network.")]
        class QueueSubOptions
        {
            [Option('n', "agent-name", HelpText = "A unique name for the build agent. If not set, a name will be generated.")]
            public string AgentName { get; set; }

            [Option('p', "port", HelpText = "Port on which to listen for build queue messages.")]
            public string Host { get; set; }
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
            // Load pipeline modules
            ModuleLoader modules = LoadModules();

            ProjectConfig projectConfig = null;

            try
            {
                var s = new Serializer();
                var converters = modules.GetJsonConverters();
                projectConfig = s.Deserialize<ProjectConfig>(opts.ProjectConfigPath, converters);
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't parse project config:");
                Console.WriteLine(e.Message);
                return 1;
            }

            if (projectConfig != null)
            {
                string outputPath = opts.OutputPath;
                if (string.IsNullOrEmpty(outputPath))
                {
                    // Config file's directory
                    outputPath = new FileInfo(opts.ProjectConfigPath).Directory.FullName;
                }

                // Find valid target
                string target = opts.BuildTarget;
                if (string.IsNullOrEmpty(target))
                {
                    // FIXME check to see if a target exists
                    target = projectConfig.BuildTargets[0].TargetName;
                }

                PipelineConfig builderConfig = new PipelineConfig { ProjectsPath = outputPath };
                PipelineRunner builder = new PipelineRunner(builderConfig);
                builder.ExecutePipeline(projectConfig, target, modules);
            }

            return 0;
        }

        // Submit job to the network
        static int Submit(SubmitSubOptions opts)
        {
            var submit = new BuildSubmit();
            submit.Submit();

            return 0;
        }

        // Listen for jobs on the network
        static int Queue(QueueSubOptions opts)
        {
            var modules = LoadModules();

            string agentName = string.IsNullOrEmpty(opts.AgentName) ? AgentName.GetUniqueAgentName() : opts.AgentName;

            var server = new BuildQueue(agentName);
            server.Start();

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

        static ModuleLoader LoadModules()
        {
            ModuleLoader modules = new ModuleLoader();
            modules.LoadAllAssemblies("./Modules");

            BuildConsole.WriteLine("Loading Pipeline Modules");
            BuildConsole.IndentLevel++;

            string line = "";

            line = "Source:      " + string.Join(", ", modules.SourceModules.Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Build:       " + string.Join(", ", modules.BuildModules.Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Archive:     " + string.Join(", ", modules.ArchiveModules.Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Distribute:  " + string.Join(", ", modules.DistributeModules.Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Notify:      " + string.Join(", ", modules.NotifyModules.Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            BuildConsole.IndentLevel--;

            Console.WriteLine("");

            return modules;
        }
    }
}
