using System;

namespace SeudoBuild
{
    public abstract class PipelineStepResults
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
    }
}
