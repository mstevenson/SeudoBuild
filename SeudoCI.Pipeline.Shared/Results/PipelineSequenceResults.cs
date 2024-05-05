using System;
using System.Collections.Generic;

namespace SeudoCI.Pipeline
{
    /// <summary>
    /// Results from an entire sequence of pipeline steps of the same type.
    /// </summary>
    public abstract class PipelineSequenceResults
    {
        /// <summary>
        /// If false, the pipeline sequence catastrophically failed during one of its steps.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// If true, the pipeilne sequence was skipped, likely due to containing no steps.
        /// </summary>
        public bool IsSkipped{ get; set; }

        /// <summary>
        /// If true, step must not be skipped. IsSuccess will be set to false if IsSkipped is true.
        /// </summary>
        public bool IsMandatory { get; set; }

        public Exception Exception { get; set; }

        public TimeSpan Duration { get; set; }
    }

    /// <inheritdoc />
    /// <summary>
    /// Results from an entire sequence of pipeline steps of the same type,
    /// given a specific results object type.
    /// </summary>
    public abstract class PipelineSequenceResults<T> : PipelineSequenceResults
        where T : PipelineStepResults, new()
    {
        public List<T> StepResults { get; } = new List<T>();
    }
}
