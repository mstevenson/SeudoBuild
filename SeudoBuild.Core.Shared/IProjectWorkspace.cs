namespace SeudoBuild
{
    public interface IProjectWorkspace : IWorkspace<ProjectDirectory>
    {
        ITargetWorkspace CreateTarget(string targetName);
    }
}
