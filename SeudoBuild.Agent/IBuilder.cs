using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    public interface IBuilder
    {
        bool IsRunning { get; }
        bool Build(ProjectConfig projectConfig, string target, string parentDirectory);
    }
}
