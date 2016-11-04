using System;
namespace UnityBuildServer
{
    public class Project
    {
        Workspace workspace;

        public static Project Create(ProjectConfig config, string projectsBaseDirectory)
        {
            return new Project(config, projectsBaseDirectory);
        }

        Project(ProjectConfig config, string projectsBaseDirectory)
        {
            string projectNameSanitized = config.Id.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";

            workspace = new Workspace
            {
                WorkingDirectory = $"{projectDirectory}/Workspace",
                IntermediateBuildDirectory = $"{projectDirectory}/Intermediate",
                ArchiveDirectory = $"{projectDirectory}/Products",
            };

            workspace.InitializeDirectories();
        }
    }
}
