using System;
namespace SeudoBuild
{
    public class SourceStepResults : PipelineStepResults
    {
        public string CommitIdentifier { get; set; }
    }
}
