using System;
namespace UnityBuildServer
{
    public class ProjectPipeline
    {
        ProjectConfig config;
        string buildTargetName;
        Workspace workspace;

        public ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            this.config = config;
            this.buildTargetName = buildTargetName;
        }

        public void Initialize(string projectsBaseDirectory)
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
        }
    }
}
