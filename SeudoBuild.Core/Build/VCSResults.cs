using System;

namespace SeudoBuild
{
    public class VCSResults
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
        public string CurrentCommitIdentifier { get; set; }
    }
}
