using System;
using System.Collections.Generic;
using SeudoBuild;
using Mono.Options;

namespace SeudoBuild.Agent
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            string outputPath = null;
            string projectConfigPath = null;
            string buildTarget = null;

            var options = new OptionSet
            {
                { "o|output-path=", "a path to the build output folder", o => outputPath = o },
                { "p|project-config=", "the path to a project configuration json file", p => projectConfigPath = p },
                { "t|build-target=", "the name of the target to build", t => buildTarget = t },
            };

            List<string> extra;
            try
            {
                extra = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
            }

            if (args.Length == 0)
            {
                var server = new BuildServer();
                server.Start();
            }
            else
            {
                if (string.IsNullOrEmpty(projectConfigPath) || string.IsNullOrEmpty(buildTarget) || !string.IsNullOrEmpty(outputPath))
                {
                    options.WriteOptionDescriptions(Console.Out);
                    return;
                }
                
                ProjectConfig projectConfig = null;
                
                try
                {
                    var s = new Serializer();
                    projectConfig = s.Deserialize<ProjectConfig>(projectConfigPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Can't parse project config:");
                    Console.WriteLine(e.Message);
                }
                
                if (projectConfig != null)
                {
                    BuilderConfig builderConfig = new BuilderConfig { ProjectsPath = outputPath };
                    Builder builder = new Builder(builderConfig);
                    builder.ExecuteBuild(projectConfig, buildTarget);
                }
            }
        }
    }
}
