namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <inheritdoc />
/// <summary>
/// A pipeline module that retrieves project source files from a repository.
/// </summary>
[ModuleCategory(typeof(ISourceStep), typeof(SourceStepConfig), "Source")]
public interface ISourceModule : IModule
{
}