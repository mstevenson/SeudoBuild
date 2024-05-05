using System;
using System.Collections.Generic;

namespace SeudoCI.Pipeline
{
    /// <summary>
    /// Defines a pipeline module, a plugin that makes new types of steps
    /// available to a build pipeline.
    /// </summary>
    public interface IModule
    {
        string Name { get; }
        Type StepType { get; }
        Type StepConfigType { get; }
        string StepConfigName { get; }
    }
}
