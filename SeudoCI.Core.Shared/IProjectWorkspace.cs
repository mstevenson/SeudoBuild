namespace SeudoCI.Core;

public interface IProjectWorkspace : IWorkspace<ProjectDirectory>
{
    ITargetWorkspace CreateTarget(string targetName);
}