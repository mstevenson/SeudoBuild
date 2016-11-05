using System.IO;
using System.Threading.Tasks;
using RunProcessAsTask;

namespace UnityBuildServer
{
    public class Workspace
    {
        /// <summary>
        /// Contains project files downloaded from a version control system.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                return workingDirectory;
            }
            set
            {
                workingDirectory = value;
                Replacements["working_directory"] = workingDirectory;
            }
        }
        string workingDirectory;

        /// <summary>
        /// Contains intermediate build files
        /// </summary>
        /// <value>The output directory.</value>
        public string BuildOutputDirectory
        {
            get
            {
                return buildOutputDirectory;
            }
            set
            {
                buildOutputDirectory = value;
                Replacements["build_output_directory"] = buildOutputDirectory;
            }
        }
        string buildOutputDirectory;

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        public string ArchivesDirectory
        {
            get
            {
                return archivesDirectory;
            }
            set
            {
                archivesDirectory = value;
                Replacements["archives_directory"] = archivesDirectory;
            }
        }
        string archivesDirectory;

        public TextReplacements Replacements { get; } = new TextReplacements();

        public Workspace(string projectDirectory)
        {
            WorkingDirectory = $"{projectDirectory}/Workspace";
            BuildOutputDirectory = $"{projectDirectory}/BuildOutput";
            ArchivesDirectory = $"{projectDirectory}/Archives";
        }

        public void CreateSubDirectories()
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }
            if (!Directory.Exists(BuildOutputDirectory))
            {
                Directory.CreateDirectory(BuildOutputDirectory);
            }
            if (!Directory.Exists(ArchivesDirectory))
            {
                Directory.CreateDirectory(ArchivesDirectory);
            }
        }

        public void CleanWorkingDirectory()
        {
            CleanDirectory(WorkingDirectory);
        }

        public void CleanBuildOutputDirectory()
        {
            CleanDirectory(BuildOutputDirectory);
        }

        public void CleanArchivesDirectory()
        {
            CleanDirectory(ArchivesDirectory);
        }

        void CleanDirectory (string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            foreach (FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
