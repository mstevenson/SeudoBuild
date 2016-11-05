using System;
using System.Collections.Generic;

namespace UnityBuildServer
{
    public class ProjectPipeline
    {
        public VCS VersionControlSystem { get; private set; }
        public List<BuildStep> BuildSteps { get; private set; }
        public List<ArchiveStep> ArchiveSteps { get; private set; }
        public List<DistributeStep> DistributeSteps { get; private set; }
        public List<NotifyStep> NotifySteps { get; private set; }

        ProjectConfig ProjectConfig { get; set; }
        BuildTargetConfig TargetConfig { get; set; }

        Workspace workspace;

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
            string projectNameSanitized = ProjectConfig.Id.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";

            workspace = new Workspace
            {
                WorkingDirectory = $"{projectDirectory}/Workspace",
                BuildOutputDirectory = $"{projectDirectory}/BuildOutput",
                ArchivesDirectory = $"{projectDirectory}/Archives",
            };

            TextReplacements.RegisterReplacement("working_directory", workspace.WorkingDirectory);
            TextReplacements.RegisterReplacement("build_output_directory", workspace.BuildOutputDirectory);
            TextReplacements.RegisterReplacement("archives_directory", workspace.ArchivesDirectory);

            workspace.InitializeDirectories();

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
                if (t.Name == targetName)
                {
                    return t;
                }
            }
            return null;
        }

        VCS InitializeVersionControlSystem()
        {
            if (TargetConfig.VCSConfiguration is GitVCSConfig)
            {
                var gitConfig = (GitVCSConfig)TargetConfig.VCSConfiguration;
                var vcs = new GitVCS(workspace.WorkingDirectory, gitConfig);
                return vcs;
            }
            throw new Exception("Could not identify VCS type from target configuration");
        }

        List<BuildStep> GenerateBuildSteps()
        {
            var steps = new List<BuildStep>();
            foreach (var stepConfig in TargetConfig.BuildSteps)
            {
                if (stepConfig is UnityBuildConfig)
                {
                    steps.Add(new UnityBuildStep((UnityBuildConfig)stepConfig, workspace));
                }
                else if (stepConfig is ShellBuildStepConfig)
                {
                    steps.Add(new ShellBuildStep((ShellBuildStepConfig)stepConfig, workspace));
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
            foreach (var stepConfig in TargetConfig.DistributeSteps)
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
