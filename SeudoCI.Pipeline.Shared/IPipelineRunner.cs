using System.Threading;

namespace SeudoCI.Pipeline;

public interface IPipelineRunner
{
    void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, IModuleLoader moduleLoader,
        CancellationToken cancellationToken = default);
}