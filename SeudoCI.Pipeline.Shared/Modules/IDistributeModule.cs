namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <inheritdoc />
/// <summary>
/// A pipeline module that distributes archives created by an IArchiveModule.
/// </summary>
[ModuleCategory(typeof(IDistributeStep), typeof(DistributeStepConfig), "Distribute")]
public interface IDistributeModule : IModule
{
}