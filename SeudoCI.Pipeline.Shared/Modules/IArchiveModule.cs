namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <inheritdoc />
/// <summary>
/// A pipeline module that creates an archive of build products.
/// </summary>
[ModuleCategory(typeof(IArchiveStep), typeof(ArchiveStepConfig), "Archive")]
public interface IArchiveModule : IModule
{
}