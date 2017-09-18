using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IFileSystem
    {
        string DocumentsPath { get; }

        List<string> GetFiles(string directoryPath, string searchPattern = null);

        bool FileExists(string path);

        void MoveFile(string source, string destination);

        void CopyFile(string source, string destination);

        void DeleteFile(string path);

        void ReplaceFile(string source, string destination, string backupDestination);

        List<string> GetDirectories(string directoryPath, string searchPattern = null);

        bool DirectoryExists(string path);

        void CreateDirectory(string path);

        void DeleteDirectory(string path);

        System.IO.Stream OpenRead(string path);

        System.IO.Stream OpenWrite(string path);

        void WriteAllText(string path, string contents);
    }
}