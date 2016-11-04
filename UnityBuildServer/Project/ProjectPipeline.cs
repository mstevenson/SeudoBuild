using System;
using System.Collections.Generic;

namespace UnityBuildServer
{
    public class ProjectPipeline
    {
        ProjectConfig config;
        string buildTargetName;
        Workspace workspace;
        BuildTargetConfig target;

        public IVCS VersionControlSystem { get; private set; }
        public List<BuildStep> BuildSteps { get; private set; }
        public List<ArchiveStep> ArchiveSteps { get; private set; }
        public List<DistributeStep> DistributeSteps { get; private set; }
        public List<NotifyStep> NotifySteps { get; private set; }

        public static ProjectPipeline Create(string baseDirectory, ProjectConfig config, string buildTargetName)
        {
            var pipeline = new ProjectPipeline(config, buildTargetName);
            pipeline.Initialize(baseDirectory);
            return pipeline;
        }

        ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            this.config = config;
            this.buildTargetName = buildTargetName;
        }

        void Initialize(string projectsBaseDirectory)
        {
            string projectNameSanitized = config.Id.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";

            workspace = new Workspace
            {
                WorkingDirectory = $"{projectDirectory}/Workspace",
                BuildProductDirectory = $"{projectDirectory}/Intermediate",
                ArchivesDirectory = $"{projectDirectory}/Products",
            };

            workspace.InitializeDirectories();

            target = GetBuildTargetConfig(buildTargetName);

            // FIXME linker appears to be stripping out GitVCS type
            //VersionControlSystem = InitializeVersionControlSystem();
            BuildSteps = GenerateBuildSteps();
            ArchiveSteps = GenerateArchiveSteps();
            DistributeSteps = GenerateDistributeSteps();
            NotifySteps = GenerateNotifySteps();
        }

        BuildTargetConfig GetBuildTargetConfig(string targetName)
        {
            foreach (var t in config.BuildTargets)
            {
                if (t.Name == buildTargetName)
                {
                    return t;
                }
            }
            return null;
        }

        IVCS InitializeVersionControlSystem()
        {
            if (target.VCSConfiguration is GitVCSConfig)
            {
                var gitConfig = (GitVCSConfig)target.VCSConfiguration;
                var vcs = new GitVCS(workspace.WorkingDirectory, gitConfig.User, gitConfig.Password, gitConfig.IsLFS);
                return vcs;
            }
            throw new Exception("Could not identify VCS type from target configuration");
        }

        List<BuildStep> GenerateBuildSteps()
        {
            var steps = new List<BuildStep>();
            foreach (var stepConfig in target.BuildSteps)
            {
                if (stepConfig is UnityBuildConfig)
                {
                    steps.Add(new UnityBuildStep((UnityBuildConfig)stepConfig));
                }
                else if (stepConfig is ShellBuildStepConfig)
                {
                    steps.Add(new ShellBuildStep((ShellBuildStepConfig)stepConfig));
                }
            }
            return steps;
        }

        List<ArchiveStep> GenerateArchiveSteps()
        {
            var steps = new List<ArchiveStep>();
            foreach (var stepConfig in target.ArchiveSteps)
            {
                if (stepConfig is ZipArchiveConfig)
                {
                    steps.Add(new ZipArchiveStep((ZipArchiveConfig)stepConfig));
                }
                else if (stepConfig is FolderArchiveStepConfig)
                {
                    steps.Add(new FolderArchiveStep((FolderArchiveStepConfig)stepConfig));
                }
            }
            return steps;
        }

        List<DistributeStep> GenerateDistributeSteps()
        {
            var steps = new List<DistributeStep>();
            foreach (var stepConfig in target.DistributeSteps)
            {
                if (stepConfig is FTPDistributeConfig)
                {
                    steps.Add(new FTPDistributeStep((FTPDistributeConfig)stepConfig));
                }
                else if (stepConfig is SteamDistributeConfig)
                {
                    steps.Add(new SteamDistributeStep((SteamDistributeConfig)stepConfig));
                }
            }
            return steps;
        }

        List<NotifyStep> GenerateNotifySteps()
        {
            var steps = new List<NotifyStep>();
            foreach (var stepConfig in target.NotifySteps)
            {
                if (stepConfig is EmailNotifyConfig)
                {
                    steps.Add(new EmailNotifyStep((EmailNotifyConfig)stepConfig));
                }
            }
            return steps;
        }
    }
}
