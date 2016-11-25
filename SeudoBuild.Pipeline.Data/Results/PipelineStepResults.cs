using System;

namespace SeudoBuild.Pipeline
{
    public abstract class PipelineStepResults
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
    }
}
