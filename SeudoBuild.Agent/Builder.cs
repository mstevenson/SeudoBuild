using SeudoBuild.Pipeline;
using System.Threading.Tasks;

namespace SeudoBuild.Agent
{
    /// <inheritdoc />
    /// <summary>
    /// Executes a build pipeline for a given project and target.
    /// </summary>
    public class Builder : IBuilder
    {
        /// <summary>
        /// Indicates whether a build is current in-progress.
        /// </summary>
        public bool IsRunning { get; private set; }

        private readonly IModuleLoader _moduleLoader;
        private readonly ILogger _logger;

        public Builder(IModuleLoader moduleLoader, ILogger logger)
        {
            _moduleLoader = moduleLoader;
            _logger = logger;
        }

        /// <summary>
        /// Execute a build for the given project and target.
        /// </summary>
        public bool Build(ProjectConfig projectConfig, string target, string outputDirectory)
        {
            if (projectConfig == null)
            {
                throw new System.ArgumentException("Could not execute build, projectConfig is null");
            }

            // Find a valid target
            if (string.IsNullOrEmpty(target))
            {
                try
                {
                    target = projectConfig.BuildTargets[0].TargetName;
                }
                catch (System.IndexOutOfRangeException)
                {
                    throw new InvalidProjectConfigException("ProjectConfig does not contain a build target.");
                }
            }

            // Execute build
            IsRunning = true;
            var pipeline = new PipelineRunner(new PipelineConfig { BaseDirectory = outputDirectory }, _logger);
            pipeline.ExecutePipeline(projectConfig, target, _moduleLoader);
            IsRunning = false;
            
            return true;
        }
    }
}
