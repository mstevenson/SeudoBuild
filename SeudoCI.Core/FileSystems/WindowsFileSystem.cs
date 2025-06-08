namespace SeudoCI.Core.FileSystems;

using System;
using System.Collections.Generic;

/// <summary>
/// Default Windows filesystem for standalone apps.
/// </summary>
public class WindowsFileSystem : IFileSystem
{
    public virtual string StandardOutputPath => "CON";

    public virtual string DocumentsPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public IEnumerable<string> GetFiles(string directoryPath, string? searchPattern = null)
    {
        // Handle missing directory
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        // Collect file paths
        var filePaths = searchPattern != null
            ? Directory.GetFiles(directoryPath, searchPattern)
            : Directory.GetFiles(directoryPath);

        var files = new List<string>(filePaths);
        return files;
    }

    public bool FileExists(string path) => File.Exists(path);

    public void MoveFile(string source, string destination) => File.Move(source, destination);

    public void CopyFile(string source, string destination) => File.Copy(source, destination);

    public void DeleteFile(string path) => File.Delete(path);

    public void ReplaceFile(string source, string destination, string backupDestination) => File.Replace(source, destination, backupDestination);

    public IEnumerable<string> GetDirectories(string directoryPath, string? searchPattern = null)
    {
        // Handle missing directory
        if (!Directory.Exists(directoryPath))
        {
            return new List<string>();
        }

        // Collect directory paths
        var dirPaths = searchPattern != null
            ? Directory.GetDirectories(directoryPath, searchPattern)
            : Directory.GetDirectories(directoryPath);

        var dirs = new List<string>(dirPaths);
        return dirs;
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);

        foreach (var file in directoryInfo.GetFiles())
        {
            file.Delete();
        }

        foreach (var dir in directoryInfo.GetDirectories())
        {
            dir.Delete(true);
        }
    }

    public Stream OpenRead(string path) => File.OpenRead(path);

    public Stream OpenWrite(string path) => File.OpenWrite(path);

    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
}