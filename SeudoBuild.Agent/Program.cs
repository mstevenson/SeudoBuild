using System;
using CommandLine;
using CommandLine.Text;
using System.IO;

namespace SeudoBuild.Agent
{
    class MainClass
    {
        class Options
        {
            [VerbOption("build", HelpText = "Create a local build.")]
            public BuildSubOptions BuildVerb { get; set; }

            [VerbOption("submit", HelpText = "Submit a build request for a remote build agent to fulfill.")]
            public SubmitSubOptions SubmitVerb { get; set; }

            [VerbOption("queue", HelpText = "Queue build requests received over the network.")]
            public QueueSubOptions QueueVerb { get; set; }

            [VerbOption("deploy", HelpText = "Listen for deployment messages.")]
            public DeploySubOptions DeployVerb { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                var help = new HelpText
                {
                    Heading = new HeadingInfo("SeudoBuild", "1.0"),
                    AdditionalNewLineAfterOption = true,
                    AddDashesToOption = true
                };
                //help.AddPreOptionsLine("Usage: app -p Someone");
                help.AddOptions(this);
                return help;
            }
        }

        class BuildSubOptions
        {
            [Option('p', "project-config", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }

            [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.", Required = true)]
            public string BuildTarget { get; set; }

            [Option('o', "output-folder", HelpText = "Path to the build output folder.")]
            public string OutputPath { get; set; }
        }

        class SubmitSubOptions
        {
            [Option('p', "project-config", HelpText = "Path to a project configuration file.", Required = true)]
            public string ProjectConfigPath { get; set; }

            [Option('t', "build-target", HelpText = "Name of the target to build as specified in the project configuration file.", Required = true)]
            public string BuildTarget { get; set; }

            [Option('h', "host", HelpText = "URI for a specific build agent. If not set, the job will be broadcast to all available agents.")]
            public string Host { get; set; }
        }

        class QueueSubOptions
        {
        }

        class DeploySubOptions
        {
        }

        public static void Main(string[] args)
        {
            string invokedVerb = null;
            object invokedVerbInstance = null;
            var options = new Options();

            if (args.Length == 0)
            {
                string help = options.GetUsage();
                Console.WriteLine(help);
                return;
            }

            var parseSuccess = Parser.Default.ParseArguments(args, options, (verb, subOptions) =>
            {
                // if parsing succeeds the verb name and correct instance
                // will be passed to onVerbCommand delegate (string,object)
                invokedVerb = verb;
                invokedVerbInstance = subOptions;
            });

            //if (!parseSuccess)
            //{
            //    Environment.Exit(Parser.DefaultExitCodeFail);
            //}

            // Build locally
            if (invokedVerb == "build")
            {
                var buildSubOptions = (BuildSubOptions)invokedVerbInstance;
                ProjectConfig projectConfig = null;

                //try
                //{
                    var s = new Serializer();
                    projectConfig = s.Deserialize<ProjectConfig>(buildSubOptions.ProjectConfigPath);
                //}
                //catch (Exception e)
                //{
                //    Console.WriteLine("Can't parse project config:");
                //    Console.WriteLine(e.Message);
                //}

                if (projectConfig != null)
                {
                    string outputPath = options.BuildVerb.OutputPath;
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        // Config file's directory
                        outputPath = new FileInfo(options.BuildVerb.ProjectConfigPath).Directory.FullName;
                    }
                    PipelineConfig builderConfig = new PipelineConfig { ProjectsPath = outputPath };
                    PipelineRunner builder = new PipelineRunner(builderConfig);
                    builder.ExecutePipeline(projectConfig, options.BuildVerb.BuildTarget);
                }
            }

            // Submit job to the network
            else if (invokedVerb == "submit")
            {
                var submitSubOptions = (SubmitSubOptions)invokedVerbInstance;
                var submit = new BuildSubmit();
                submit.Submit();
            }

            // Listen for jobs on the network
            else if (invokedVerb == "listen")
            {
                var listenSubOptions = (QueueSubOptions)invokedVerbInstance;
                var server = new BuildQueue();
                server.Start();
            }
        }
    }
}
