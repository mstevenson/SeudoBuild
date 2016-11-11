using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    //public abstract class PipelineSequenceResults
    //{
    //}

    public abstract class PipelineSequenceResults<TStep>
        where TStep : PipelineStepResults, new()
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
        public List<TStep> StepResults { get; } = new List<TStep>();
    }
}
