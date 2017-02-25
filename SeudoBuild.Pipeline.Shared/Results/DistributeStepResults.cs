using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Results from distributing an archive of a build product.
    /// </summary>
    public class DistributeStepResults : PipelineStepResults
    {
        public class FileResult
        {
            public ArchiveStepResults ArchiveInfo { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        /// <summary>
        /// Result of each archived file that was distributed.
        /// </summary>
        public List<FileResult> FileResults { get; } = new List<FileResult> ();
    }
}
