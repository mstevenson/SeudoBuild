using System;

namespace SeudoBuild
{
    public class SourceSequenceResults : PipelineSequenceResults<SourceStepResults>
    {
        public string CurrentCommitIdentifier { get; set; }
    }
}
