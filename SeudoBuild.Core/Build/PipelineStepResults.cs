using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    public abstract class PipelineStepResults
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
    }

    public abstract class PipelineStepResults<T> : PipelineStepResults
    {
        public List<T> Infos { get; } = new List<T>();
    }
}
