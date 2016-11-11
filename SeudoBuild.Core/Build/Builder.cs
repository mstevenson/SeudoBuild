using System;
using System.Collections.Generic;
using SeudoBuild.VCS;
using System.Diagnostics;
using System.Linq;

namespace SeudoBuild
{
    /// <summary>
    /// 
    /// </summary>
    public class Builder
    {
        BuilderConfig builderConfig;

        public Builder(BuilderConfig config)
        {
            this.builderConfig = config;
        }

        public void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName)
        {
            if (projectConfig == null)
            {
                throw new ArgumentNullException(nameof(projectConfig), "A project configuration definition must be specified.");
            }
            if (string.IsNullOrEmpty (buildTargetName))
            {
                throw new ArgumentNullException(nameof(buildTargetName), "A project configuration definition must be specified.");
            }

            BuildTargetConfig targetConfig = projectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
            if (targetConfig == null)
            {
                throw new ArgumentException("The specified build target could not be found in the project.", "buildTargetName");
            }

            Console.WriteLine("Starting build process:");

            BuildConsole.WriteLine($"Project: {projectConfig.ProjectName}");
            BuildConsole.WriteLine($"Target:  {buildTargetName}");
            BuildConsole.WriteLine("");

            // Setup pipelin
            var pipeline = ProjectPipeline.Create(builderConfig.ProjectsPath, projectConfig, buildTargetName);
            var replacements = pipeline.Workspace.Replacements;

            // Grab changes from version control system
            VCSResults vcsResults = UpdateWorkingCopy(pipeline);

            // TODO handle VCS failure, pass the result through to the Build step

            // Build
            var buildResults = Build(vcsResults, pipeline);
            var step = buildResults.StepResults[0];
            replacements["project_name"] = step.ProjectName;
            replacements["build_target_name"] = step.BuildTargetName;
            replacements["app_version"] = step.AppVersion.ToString ();
            replacements["build_date"] = step.BuildDate.ToString("yyyy-dd-M--HH-mm-ss");
            replacements["commit_identifier"] = step.CommitIdentifier;

            // Archive
            var archiveResults = Archive(buildResults, pipeline);

            // Distribute
            var distributeResults = Distribute(archiveResults, pipeline);

            // Notify
            var notifyresults = Notify(distributeResults, pipeline);

            // Done
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("");
            Console.WriteLine("Build process completed.");
        }

        VCSResults UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VersionControlSystem vcs = pipeline.VersionControlSystem;

            BuildConsole.WriteBullet($"Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel++;

            var results = new VCSResults();

            try
            {
                if (vcs.IsWorkingCopyInitialized)
                {
                    vcs.Update();
                }
                else
                {
                    vcs.Download();
                }
            }
            catch (Exception e)
            {
                results.Success = false;
                results.Exception = e;
            }

            results.CurrentCommitIdentifier = vcs.CurrentCommit;
            results.Success = true;
            return results;
        }

        BuildSequenceResults Build(VCSResults vcsResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Build");
            BuildConsole.IndentLevel = 1;

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No build steps");
                //return new BuildStepResults { Status = BuildCompletionStatus.Faulted };
                return new BuildSequenceResults { IsSuccess = false, Exception = new Exception("No build steps.") };
            }

            bool isFaulted = false;

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            var sequenceResults = new BuildSequenceResults();

            // TODO add app version
            var stepResults = new BuildStepResults
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.TargetName,
                CommitIdentifier = vcsResults.CurrentCommitIdentifier,
                //Status = BuildCompletionStatus.Running
            };

            sequenceResults.StepResults.Add(stepResults);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            IBuildStep currentStep = null;
            int stepIndex = 0;

            // Build
            foreach (var step in pipeline.BuildSteps)
            {
                stepIndex++;
                currentStep = step;
                BuildConsole.WriteBullet($"{step.Type} (step {stepIndex}/{pipeline.BuildSteps.Count})");
                BuildConsole.IndentLevel++;

                var stepResult = step.Execute();

                BuildConsole.IndentLevel--;
                if (stepResult.Status == BuildCompletionStatus.Faulted)
                {
                    isFaulted = true;
                    //buildInfo.Status = BuildCompletionStatus.Faulted;
                    break;
                }
            }

            stopwatch.Stop();
            stepResults.BuildDuration = stopwatch.Elapsed;

            if (!isFaulted)
            {
                //buildInfo.Status = BuildCompletionStatus.Completed;
                sequenceResults.IsSuccess = true;
                BuildConsole.WriteSuccess("Build steps completed in " + stepResults.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                sequenceResults.IsSuccess = false;
                string error = $"Build failed on step {stepIndex} ({currentStep.Type})";
                sequenceResults.Exception = new Exception(error);
                BuildConsole.WriteFailure(error);
            }

            return sequenceResults;
        }

        ArchiveSequenceResults Archive(BuildSequenceResults buildResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Archive");
            BuildConsole.IndentLevel++;

            var results = new ArchiveSequenceResults();

            if (pipeline.ArchiveSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No archive steps");
                results.IsSuccess = false;
                results.Exception = new Exception("No archive steps.");
                return results;
            }

            if (!buildResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous build step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped archival, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.ArchiveSteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.ExecuteStep(buildResults, pipeline.Workspace);
                results.StepResults.Add(info);
            }

            return results;
        }

        DistributeSequenceResults Distribute(ArchiveSequenceResults archiveResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Distribute");
            BuildConsole.IndentLevel++;

            var results = new DistributeSequenceResults();

            if (pipeline.DistributeSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No distribute steps");
                results.IsSuccess = false;
                results.Exception = new Exception("No distribute steps.");
                return results;
            }

            if (!archiveResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous build step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped distribute, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.DistributeSteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.ExecuteStep(archiveResults, pipeline.Workspace);
                results.StepResults.Add(info);
            }

            return results;
        }

        NotifySequenceResults Notify(DistributeSequenceResults distributeResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Notify");
            BuildConsole.IndentLevel++;

            if (pipeline.NotifySteps.Count == 0)
            {
                BuildConsole.WriteAlert("No notify steps");
            }

            // TODO handle faulted state from the previous step, bail out, forward the error

            BuildConsole.IndentLevel++;

            var results = new NotifySequenceResults();

            if (!distributeResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped distribute, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.NotifySteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.ExecuteStep(distributeResults, pipeline.Workspace);
                results.StepResults.Add(info);
            }

            BuildConsole.IndentLevel--;

            return results;
        }

        //T ExecuteSequence<T, U, V>(string sequenceName, List<PipelineStep<U,V>> sequenceSteps, U previousSequenceResults, Workspace workspace)
        //    where T : PipelineSequenceResults, new() // current sequence return value
        //    where U : PipelineSequenceResults, new() // previous sequence results
        //    where V : PipelineStepResults, new() // step info result type
        //{
        //    BuildConsole.IndentLevel = 0;
        //    BuildConsole.WriteBullet(sequenceName);
        //    BuildConsole.IndentLevel++;


        //    var results = new T();

        //    if (sequenceSteps.Count == 0)
        //    {
        //        BuildConsole.WriteAlert($"No {sequenceName} steps");
        //        results.IsSuccess = false;
        //        results.Exception = new Exception($"No {sequenceName} steps.");
        //        return results;
        //    }

        //    if (!previousSequenceResults.IsSuccess)
        //    {
        //        BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
        //        results.IsSuccess = false;
        //        results.Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.");
        //        return results;
        //    }

        //    foreach (var step in sequenceSteps)
        //    {
        //        BuildConsole.WriteBullet(step.Type);
        //        var info = step.ExecuteStep(previousSequenceResults, workspace);
        //        results.Infos.Add(info);
        //    }

        //    return results;
        //}
    }
}
