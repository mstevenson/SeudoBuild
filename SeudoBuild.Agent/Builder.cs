using System;
using System.IO;
using System.Threading.Tasks;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    public class Builder
    {
        public bool IsRunning { get; private set; }

        public bool Build(ProjectConfig projectConfig, string target, string parentDirectory, ModuleLoader modules, ILogger logger)
        {
            if (projectConfig != null)
            {
                // Find valid target
                if (string.IsNullOrEmpty(target))
                {
                    // FIXME check to see if a target exists
                    target = projectConfig.BuildTargets[0].TargetName;
                }

                Task.Factory.StartNew(() =>
                {
                    IsRunning = true;
                    PipelineConfig pipelineConfig = new PipelineConfig { ProjectsPath = parentDirectory };
                    PipelineRunner pipelineBuilder = new PipelineRunner(pipelineConfig, logger);
                    pipelineBuilder.ExecutePipeline(projectConfig, target, modules);
                    IsRunning = false;
                });
            }

            IsRunning = false;
            return true;
        }
    }
}
