using System;
using System.Collections.Generic;

namespace SeudoBuild.Core.FileSystems
{
    /// <summary>
    /// Default Windows filesystem for standalone apps.
    /// </summary>
    public class WindowsFileSystem : IFileSystem
    {
        public virtual string StandardOutputPath { get; } = "CON";
        
        public virtual string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public IEnumerable<string> GetFiles(string directoryPath, string searchPattern = null)
        {
            // Handle missing directory
            if (!System.IO.Directory.Exists(directoryPath))
            {
                return new List<string>();
            }

            // Collect file paths
            var filePaths = searchPattern != null
                ? System.IO.Directory.GetFiles(directoryPath, searchPattern)
                : System.IO.Directory.GetFiles(directoryPath);

            var files = new List<string>(filePaths);
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

        public IEnumerable<string> GetDirectories(string directoryPath, string searchPattern = null)
        {
            // Handle missing directory
            if (!System.IO.Directory.Exists(directoryPath))
            {
                return new List<string>();
            }

            // Collect directory paths
            var dirPaths = searchPattern != null
                ? System.IO.Directory.GetDirectories(directoryPath, searchPattern)
                : System.IO.Directory.GetDirectories(directoryPath);

            var dirs = new List<string>(dirPaths);
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

            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directoryInfo.GetDirectories())
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

        public void WriteAllText(string path, string contents)
        {
            System.IO.File.WriteAllText(path, contents);
        }
    }
}