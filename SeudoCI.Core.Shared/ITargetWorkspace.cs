namespace SeudoCI.Core;

public interface ITargetWorkspace : IWorkspace<TargetDirectory>
{
    IProjectWorkspace ProjectWorkspace { get; set; }
}