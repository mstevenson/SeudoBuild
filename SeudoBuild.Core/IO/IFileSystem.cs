using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IFileSystem
    {
        string DocumentsPath { get; }

        List<string> GetFiles(string path, string searchPattern);

        bool FileExists(string path);

        void MoveFile(string source, string destination);

        void CopyFile(string source, string destination);

        void DeleteFile(string path);

        void ReplaceFile(string source, string destination, string backupDestination);

        System.IO.Stream OpenRead(string path);

        System.IO.Stream OpenWrite(string path);
    }
}