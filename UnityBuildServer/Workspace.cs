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
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Contains intermediate build files
        /// </summary>
        /// <value>The output directory.</value>
        public string BuildOutputDirectory { get; set; }

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        public string ArchivesDirectory { get; set; }

        public TextReplacements Replacements { get; set; } = new TextReplacements();

        public void InitializeDirectories()
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
