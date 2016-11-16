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

    public abstract class PipelineSequenceResults<T> : PipelineSequenceResults
        where T : PipelineStepResults, new()
    {
        public List<T> StepResults { get; } = new List<T>();
    }
}
