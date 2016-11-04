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
        public string BuildProductDirectory { get; set; }

        /// <summary>
        /// Contains products resulting from a build.
        /// </summary>
        public string ArchivesDirectory { get; set; }

        public void InitializeDirectories()
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }
            if (!Directory.Exists(BuildProductDirectory))
            {
                Directory.CreateDirectory(BuildProductDirectory);
            }
            if (!Directory.Exists(ArchivesDirectory))
            {
                Directory.CreateDirectory(ArchivesDirectory);
            }
        }

        /// <summary>
        /// Delete all files and directories in the workspace.
        /// </summary>
        public void Clean()
        {
            if (!Directory.Exists(WorkingDirectory))
            {
                return;
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(WorkingDirectory);

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
