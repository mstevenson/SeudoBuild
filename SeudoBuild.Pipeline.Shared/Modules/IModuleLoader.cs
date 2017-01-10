namespace SeudoBuild.Pipeline
{
    public interface IModuleLoader
    {
        IModuleRegistry Registry { get; }
        T CreatePipelineStep<T>(StepConfig config, IWorkspace workspace) where T : IPipelineStep;
    }
}
