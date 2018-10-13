namespace SeudoBuild.Pipeline
{
    public interface IPipelineRunner
    {
        void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, IModuleLoader moduleLoader);
    }
}