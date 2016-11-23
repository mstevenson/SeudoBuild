using System;
using System.IO;

namespace SeudoBuild.Agent
{
    public class Builder
    {
        public bool IsRunning { get; private set; }

        public bool Build(ProjectConfig projectConfig, string target, string projectConfigPath, string outputPath, ModuleLoader modules)
        {
            if (projectConfig != null)
            {
                if (string.IsNullOrEmpty(outputPath))
                {
                    // Config file's directory
                    outputPath = new FileInfo(projectConfigPath).Directory.FullName;
                }

                // Find valid target
                if (string.IsNullOrEmpty(target))
                {
                    // FIXME check to see if a target exists
                    target = projectConfig.BuildTargets[0].TargetName;
                }

                IsRunning = true;

                PipelineConfig builderConfig = new PipelineConfig { ProjectsPath = outputPath };
                PipelineRunner builder = new PipelineRunner(builderConfig);
                builder.ExecutePipeline(projectConfig, target, modules);
            }

            return true;
        }
    }
}
