namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <inheritdoc />
/// <summary>
/// A pipeline module that creates build products from source files
/// that were retrieved by an ISourceModule.
/// </summary>
[ModuleCategory(typeof(IBuildStep), typeof(BuildStepConfig), "Build")]
public interface IBuildModule : IModule
{
}