using System;
using System.Collections.Generic;
using SeudoBuild.VCS;
using SeudoBuild.VCS.Git;

namespace SeudoBuild
{
    public class ProjectPipeline
    {
        public VersionControlSystem VersionControlSystem { get; private set; }
        public List<IBuildStep> BuildSteps { get; private set; }
        public List<ArchiveStep> ArchiveSteps { get; private set; }
        public List<DistributeStep> DistributeSteps { get; private set; }
        public List<NotifyStep> NotifySteps { get; private set; }

        public ProjectConfig ProjectConfig { get; private set; }
        public BuildTargetConfig TargetConfig { get; private set; }

        public Workspace Workspace { get; private set; }

        public static ProjectPipeline Create(string baseDirectory, ProjectConfig config, string buildTargetName)
        {
            var pipeline = new ProjectPipeline(config, buildTargetName);
            pipeline.Initialize(baseDirectory, buildTargetName);
            return pipeline;
        }

        ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            this.ProjectConfig = config;
        }

        void Initialize(string projectsBaseDirectory, string buildTargetName)
        {
            string projectNameSanitized = ProjectConfig.ProjectName.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";

            Workspace = new Workspace(projectDirectory);
            BuildConsole.WriteLine("Building to " + projectDirectory);
            Workspace.CreateSubDirectories();

            TargetConfig = GetBuildTargetConfig(buildTargetName);

            VersionControlSystem = InitializeVersionControlSystem();
            BuildSteps = GenerateBuildSteps();
            ArchiveSteps = GenerateArchiveSteps();
            DistributeSteps = GenerateDistributeSteps();
            NotifySteps = GenerateNotifySteps();
        }

        BuildTargetConfig GetBuildTargetConfig(string targetName)
        {
            foreach (var t in ProjectConfig.BuildTargets)
            {
                if (t.TargetName == targetName)
                {
                    return t;
                }
            }
            return null;
        }

        VersionControlSystem InitializeVersionControlSystem()
        {
            if (TargetConfig.VCSConfiguration is GitVCSConfig)
            {
                var gitConfig = (GitVCSConfig)TargetConfig.VCSConfiguration;
                var vcs = new GitVCS(Workspace, gitConfig);
                return vcs;
            }
            throw new Exception("Could not identify VCS type from target configuration");
        }

        List<IBuildStep> GenerateBuildSteps()
        {
            var steps = new List<IBuildStep>();
            foreach (var stepConfig in TargetConfig.BuildSteps)
            {
                if (stepConfig is UnityStandardBuildConfig)
                {
                    steps.Add(new UnityStandardBuildStep((UnityStandardBuildConfig)stepConfig, Workspace));
                }
                if (stepConfig is UnityExecuteMethodBuildConfig)
                {
                    steps.Add(new UnityExecuteMethodBuildStep((UnityExecuteMethodBuildConfig)stepConfig, Workspace));
                }
                if (stepConfig is UnityParameterizedBuildConfig)
                {
                    steps.Add(new UnityParameterizedBuildStep((UnityParameterizedBuildConfig)stepConfig, Workspace));
                }
                else if (stepConfig is ShellBuildStepConfig)
                {
                    steps.Add(new ShellBuildStep((ShellBuildStepConfig)stepConfig, Workspace));
                }
            }
            return steps;
        }

        List<ArchiveStep> GenerateArchiveSteps()
        {
            var steps = new List<ArchiveStep>();
            foreach (var stepConfig in TargetConfig.ArchiveSteps)
            {
                if (stepConfig is ZipArchiveConfig)
                {
                    steps.Add(new ZipArchiveStep((ZipArchiveConfig)stepConfig));
                }
                else if (stepConfig is FolderArchiveConfig)
                {
                    steps.Add(new FolderArchiveStep((FolderArchiveConfig)stepConfig));
                }
            }
            return steps;
        }

        List<DistributeStep> GenerateDistributeSteps()
        {
            var steps = new List<DistributeStep>();
            foreach (var stepConfig in TargetConfig.DistributeSteps)
            {
                if (stepConfig is FTPDistributeConfig)
                {
                    steps.Add(new FTPDistributeStep((FTPDistributeConfig)stepConfig));
                }
                if (stepConfig is SFTPDistributeConfig)
                {
                    steps.Add(new SFTPDistributeStep((SFTPDistributeConfig)stepConfig));
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
            foreach (var stepConfig in TargetConfig.NotifySteps)
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
