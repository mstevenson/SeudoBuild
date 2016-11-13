using System;
using SeudoBuild.VCS;

namespace SeudoBuild
{
    //public abstract class SourceStepBase : ISourceStep
    //{
    //    public abstract string Type { get; }

    //    public abstract SourceStepResults ExecuteStep(Workspace workspace);

    //    //// FIXME move UpdateWorkingCopy into a SourceStep
    //    //SourceSequenceResults UpdateWorkingCopy(ProjectPipeline pipeline)
    //    //{
    //    //    //replacements["commit_identifier"] = vcsResults.CurrentCommitIdentifier;


    //    //    VersionControlSystem vcs = pipeline.VersionControlSystem;

    //    //    BuildConsole.WriteBullet($"Update working copy ({vcs.TypeName})");
    //    //    BuildConsole.IndentLevel++;

    //    //    var results = new SourceSequenceResults();

    //    //    try
    //    //    {
    //    //        if (vcs.IsWorkingCopyInitialized)
    //    //        {
    //    //            vcs.Update();
    //    //        }
    //    //        else
    //    //        {
    //    //            vcs.Download();
    //    //        }
    //    //    }
    //    //    catch (Exception e)
    //    //    {
    //    //        results.IsSuccess = false;
    //    //        results.Exception = e;
    //    //    }

    //    //    results.CurrentCommitIdentifier = vcs.CurrentCommit;
    //    //    results.IsSuccess = true;
    //    //    return results;
    //    //}
    //}
}
