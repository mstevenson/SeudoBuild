using System;
namespace UnityBuild
{
    public interface IBuildStep
    {
        string Type { get; }
        BuildResult Execute();
    }
}
