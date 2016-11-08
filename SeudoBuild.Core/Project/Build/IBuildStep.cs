using System;
namespace SeudoBuild
{
    public interface IBuildStep
    {
        string Type { get; }
        BuildResult Execute();
    }
}
