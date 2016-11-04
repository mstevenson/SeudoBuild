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
        public List<DistributionStep> DistributionSteps { get; private set; }
        public List<NotificationStep> NotificationSteps { get; private set; }

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
            DistributionSteps = GenerateDistributionSteps();
            NotificationSteps = GenerateNotificationSteps();
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
            if (target.VCSConfiguration is GitVCSConfiguration)
            {
                var gitConfig = (GitVCSConfiguration)target.VCSConfiguration;
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
                if (stepConfig is UnityBuildStepConfig)
                {
                    steps.Add(new UnityBuildStep((UnityBuildStepConfig)stepConfig));
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
                if (stepConfig is ZipArchiveStepConfig)
                {
                    steps.Add(new ZipArchiveStep((ZipArchiveStepConfig)stepConfig));
                }
                else if (stepConfig is FolderArchiveStepConfig)
                {
                    steps.Add(new FolderArchiveStep((FolderArchiveStepConfig)stepConfig));
                }
            }
            return steps;
        }

        List<DistributionStep> GenerateDistributionSteps()
        {
            var steps = new List<DistributionStep>();
            foreach (var stepConfig in target.DistributionSteps)
            {
                if (stepConfig is FTPDistributionConfig)
                {
                    steps.Add(new FTPDistributionStep((FTPDistributionConfig)stepConfig));
                }
                else if (stepConfig is SteamDistributionConfig)
                {
                    steps.Add(new SteamDistributionStep((SteamDistributionConfig)stepConfig));
                }
            }
            return steps;
        }

        List<NotificationStep> GenerateNotificationSteps()
        {
            var steps = new List<NotificationStep>();
            foreach (var stepConfig in target.NotificationSteps)
            {
                if (stepConfig is EmailNotificationConfig)
                {
                    steps.Add(new EmailNotificationStep((EmailNotificationConfig)stepConfig));
                }
            }
            return steps;
        }
    }
}
