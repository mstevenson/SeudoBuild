namespace SeudoCI.Core.FileSystems;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Default Windows filesystem for standalone apps.
/// </summary>
public class WindowsFileSystem : IFileSystem
{
    public virtual string StandardOutputPath => "CON";

    public virtual string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public IEnumerable<string> GetFiles(string directoryPath, string? searchPattern = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }
        
        return searchPattern != null
            ? Directory.GetFiles(directoryPath, searchPattern)
            : Directory.GetFiles(directoryPath);
    }

    public bool FileExists(string path) => File.Exists(path);

    public void MoveFile(string source, string destination) => File.Move(source, destination);

    public void CopyFile(string source, string destination) => File.Copy(source, destination, overwrite: false);

    public void DeleteFile(string path) => File.Delete(path);

    public void ReplaceFile(string source, string destination, string backupDestination) => File.Replace(source, destination, backupDestination);

    public IEnumerable<string> GetDirectories(string directoryPath, string? searchPattern = null)
    {
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        return searchPattern != null
            ? Directory.GetDirectories(directoryPath, searchPattern)
            : Directory.GetDirectories(directoryPath);
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    public Stream OpenRead(string path) => File.OpenRead(path);

    public Stream OpenWrite(string path) => File.OpenWrite(path);

    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
}