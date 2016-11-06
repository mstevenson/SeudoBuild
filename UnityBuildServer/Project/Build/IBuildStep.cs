using System;
namespace UnityBuild
{
    public interface IBuildStep
    {
        string TypeName { get; }
        BuildResult Execute();
    }
}
