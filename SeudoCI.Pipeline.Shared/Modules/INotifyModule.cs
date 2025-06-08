namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <inheritdoc />
/// <summary>
/// A pipeline module that publishes a notification after all other
/// pipeline steps have finished.
/// </summary>
[ModuleCategory(typeof(INotifyStep), typeof(NotifyStepConfig), "Notify")]
public interface INotifyModule : IModule
{
}