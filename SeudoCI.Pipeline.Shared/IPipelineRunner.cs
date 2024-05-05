namespace SeudoCI.Pipeline
{
    public interface IPipelineRunner
    {
        void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, IModuleLoader moduleLoader);
    }
}