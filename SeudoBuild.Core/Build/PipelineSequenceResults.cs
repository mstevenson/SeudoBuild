using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    public abstract class PipelineSequenceResults
    {
        public bool IsSuccess { get; set; }
        public Exception Exception { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public abstract class PipelineSequenceResults<TStep> : PipelineSequenceResults
        where TStep : PipelineStepResults, new()
    {
        public List<TStep> StepResults { get; } = new List<TStep>();
    }
}
