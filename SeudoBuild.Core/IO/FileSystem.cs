using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    /// <summary>
    /// Default Windows and Mac filesystem for standalone apps.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        public string DocumentsPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (Workspace.RunningPlatform == Platform.Mac)
                {
                    path += "/Documents";
                }
                return path;
            }
        }

        public List<string> GetFiles(string directoryPath, string searchPattern = null)
        {
            // Handle missing directory
            if (!System.IO.Directory.Exists(directoryPath))
            {
                return new List<string>();
            }

            // Collect file paths
            string[] filePaths;
            if (searchPattern != null)
            {
                filePaths = System.IO.Directory.GetFiles(directoryPath, searchPattern);
            }
            else {
                filePaths = System.IO.Directory.GetFiles(directoryPath);
            }

            List<string> files = new List<string>(filePaths);
            return files;
        }

        public bool FileExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        public void MoveFile(string source, string destination)
        {
            System.IO.File.Move(source, destination);
        }

        public void CopyFile(string source, string destination)
        {
            System.IO.File.Copy(source, destination);
        }

        public void DeleteFile(string path)
        {
            System.IO.File.Delete(path);
        }

        public void ReplaceFile(string source, string destination, string backupDestination)
        {
            System.IO.File.Replace(source, destination, backupDestination);
        }

        public List<string> GetDirectories(string directoryPath, string searchPattern = null)
        {
            // Handle missing directory
            if (!System.IO.Directory.Exists(directoryPath))
            {
                return new List<string>();
            }

            // Collect directory paths
            string[] dirPaths;
            if (searchPattern != null)
            {
                dirPaths = System.IO.Directory.GetDirectories(directoryPath, searchPattern);
            }
            else
            {
                dirPaths = System.IO.Directory.GetDirectories(directoryPath);
            }

            List<string> dirs = new List<string>(dirPaths);
            return dirs;
        }

        public bool DirectoryExists(string path)
        {
            return System.IO.Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            System.IO.Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string directoryPath)
        {
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(directoryPath);

            foreach (System.IO.FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (System.IO.DirectoryInfo dir in directoryInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public System.IO.Stream OpenRead(string path)
        {
            var stream = System.IO.File.OpenRead(path);
            return stream;
        }

        public System.IO.Stream OpenWrite(string path)
        {
            var stream = System.IO.File.OpenWrite(path);
            return stream;
        }
    }
}