namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// A pipeline module that publishes a notification after all other
    /// pipeline steps have finished.
    /// </summary>
    public interface INotifyModule : IModule
    {
    }
}
