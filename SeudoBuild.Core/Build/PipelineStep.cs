using System;
namespace SeudoBuild
{
    public abstract class PipelineStep<T, U, V>
        where T : PipelineSequenceResults<U> // previous sequence's result type
        where U : PipelineStepResults // previous sequence's step result type
        where V : PipelineStepResults // this sequence's step result type
    {
        public abstract string Type { get; }
        public abstract V ExecuteStep(T previousResults, Workspace workspace);
    }
}
