using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    //public abstract class PipelineSequenceResults
    //{
    //}

    public abstract class PipelineSequenceResults<T>
        where T : PipelineStepResults
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
        public List<T> StepResults { get; } = new List<T>();
    }
}
