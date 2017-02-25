using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Executes a build pipeline for a given project and target.
    /// </summary>
    public interface IBuilder
    {
        bool IsRunning { get; }
        bool Build(ProjectConfig projectConfig, string target, string parentDirectory);
    }
}
