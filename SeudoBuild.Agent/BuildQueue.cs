using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SeudoBuild.Agent
{
    public class BuildQueue : IBuildQueue
    {
        public BuildRequest ActiveBuild { get; private set; }

        ConcurrentQueue<BuildRequest> QueuedBuilds { get; } = new ConcurrentQueue<BuildRequest>();
        ConcurrentDictionary<int, BuildResult> Builds { get; } = new ConcurrentDictionary<int, BuildResult>();

        CancellationTokenSource tokenSource;

        Builder builder;

        int buildIndex;

        public void StartQueue(Builder builder)
        {
            this.builder = builder;
            tokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(TaskQueuePump, tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        void TaskQueuePump()
        {
            while (true)
            {
                if (tokenSource.IsCancellationRequested)
                {
                    return;
                }
                if (!builder.IsRunning && QueuedBuilds.Count > 0)
                {
                    BuildRequest request = null;
                    if (QueuedBuilds.TryDequeue(out request))
                    {
                        ActiveBuild = request;
                        Console.WriteLine($"Building #{request.Id}: project {request.ProjectConfiguration.ProjectName}, target {request.TargetName}");
                        // FIXME build
                        //builder.Build(ActiveBuild.ProjectConfiguration, ActiveBuild.TargetName, 
                    }
                }
                Thread.Sleep(200);
            }
        }

        public BuildRequest Build(ProjectConfig config, string target = null)
        {
            buildIndex++;
            var request = new BuildRequest (buildIndex) { ProjectConfiguration = config, TargetName = target };
            var result = new BuildResult
            {
                BuildStatus = BuildResult.Status.Queued,
                ProjectName = request.ProjectConfiguration.ProjectName,
                Id = request.Id,
                TargetName = target
            };

            QueuedBuilds.Enqueue(request);
            Builds.TryAdd(result.Id, result);

            return request;
        }

        public List<BuildResult> GetAllBuildResults()
        {
            return Builds.Values.ToList();
        }

        public BuildResult GetBuildResult(int buildId)
        {
            BuildResult result = null;
            Builds.TryGetValue(buildId, out result);
            return result;
        }

        public void CancelBuild(int buildId)
        {
            if (ActiveBuild != null && ActiveBuild.Id == buildId)
            {
                ActiveBuild = null;
                // TODO signal build process to stop
            }

            BuildResult result = null;
            if (Builds.TryGetValue(buildId, out result))
            {
                result.BuildStatus = BuildResult.Status.Cancelled;
            }
        }
    }
}
