using System.Collections.Generic;
using System.IO;

namespace UnityBuildServer
{
    public class FolderArchiveStep : ArchiveStep
    {
        FolderArchiveStepConfig config;

        public FolderArchiveStep(FolderArchiveStepConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "Folder";
            }
        }

        public override ArchiveInfo CreateArchive(BuildInfo buildInfo, Workspace workspace)
        {
            string folderName = workspace.Replacements.ReplaceVariablesInText(config.FolderName);
            string source = workspace.BuildOutputDirectory;
            string dest = $"{workspace.ArchivesDirectory}/{folderName}";

            // Remove old directory
            if (Directory.Exists(dest))
            {
                Directory.Delete(dest, true);
            }

            CopyDirectory(source, dest);

            var archiveInfo = new ArchiveInfo { ArchiveFileName = folderName };
            return archiveInfo;
        }

        //public enum Element
        //{
        //    ProjectName,
        //    BuildTargetName,
        //    BuildDate,
        //    CommitIdentifier,
        //    AppVersion
        //}

        //public string GenerateFileName(BuildInfo buildInfo, params Element[] elements)
        //{
        //    if (elements.Length == 0)
        //    {
        //        return GenerateFileName(buildInfo, Element.ProjectName, Element.BuildTargetName);
        //    }

        //    var parts = new List<string>();

        //    foreach (var e in elements)
        //    {
        //        switch (e)
        //        {
        //            case Element.ProjectName:
        //                parts.Add(buildInfo.ProjectName);
        //                break;
        //            case Element.BuildTargetName:
        //                parts.Add(buildInfo.BuildTargetName);
        //                break;
        //            case Element.BuildDate:
        //                parts.Add(buildInfo.BuildDate.ToString("yyyy-dd-M--HH-mm-ss"));
        //                break;
        //            case Element.CommitIdentifier:
        //                parts.Add(buildInfo.CommitIdentifier);
        //                break;
        //            case Element.AppVersion:
        //                parts.Add(buildInfo.AppVersion.ToString());
        //                break;
        //        }
        //    }

        //    string output = string.Join("_", parts.ToArray()).Replace(' ', '_');
        //    return output;
        //}


        void CopyDirectory(string source, string dest)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + source);
            }

            DirectoryInfo[] dirs = sourceDir.GetDirectories();
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(dest, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy subdirectories and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dest, subdir.Name);
                CopyDirectory(subdir.FullName, temppath);
            }
        }
    }
}
