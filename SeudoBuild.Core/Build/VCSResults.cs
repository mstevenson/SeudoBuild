using System;

namespace SeudoBuild
{
    public class VCSResults : PipelineSequenceResults
    {
        public string CurrentCommitIdentifier { get; set; }
    }
}
