using System.Text;

namespace SeudoCI.Core.Tests;

/// <summary>
/// A file stream that isn't closed after reading or writing, and flushes to a byte array.
/// </summary>
public class MockFile
{
    public class MockMemoryStream : MemoryStream
    {
        private readonly Action<byte[]> _flushCallback;

        public MockMemoryStream(Action<byte[]> flushCallback)
        {
            _flushCallback = flushCallback;
        }

        public MockMemoryStream(byte[] buffer, Action<byte[]> flushCallback) : base(buffer)
        {
            _flushCallback = flushCallback;
        }

        public override void Flush()
        {
            base.Flush();
            _flushCallback(base.ToArray());
        }

        protected override void Dispose(bool disposing)
        {
            Flush();
            base.Dispose(disposing);
        }
    }

    public byte[] Data { get; private set; }

    private MockMemoryStream? _writeStream;

    public MockFile()
    {
        Data = [];
    }

    public MockFile(byte[] data)
    {
        Data = data;
    }

    public Stream OpenRead()
    {
        if (_writeStream is { })
        {
            _writeStream.Flush();
            Data = _writeStream.ToArray();
        }

        return new MockMemoryStream(Data, OnFlush);
    }

    public Stream OpenWrite()
    {
        if (_writeStream is { CanWrite: true })
        {
            throw new IOException("Can't write to mock file, the mock file stream is already open");
        }

        _writeStream = new MockMemoryStream(OnFlush);
        return _writeStream;
    }

    private void OnFlush(byte[] bytes)
    {
        Data = bytes;
    }
}

/// <summary>
/// An imaginary file system that only exists in memory.
/// </summary>
public class MockFileSystem : IFileSystem
{
    private readonly Dictionary<string, MockFile> _allFiles = new(StringComparer.Ordinal);
    private readonly HashSet<string> _directories = new(StringComparer.Ordinal);

    public string TemporaryFilesPath => "/temp";

    public string StandardOutputPath { get; } = "/dev/null";

    public string DocumentsPath => "/documents";

    public IEnumerable<string> GetFiles(string directoryPath, string? searchPattern = null)
    {
        var foundFiles = new List<string>();
        var normalizedDirectory = NormalizePath(directoryPath);

        foreach (var file in _allFiles.Keys)
        {
            // TODO implement search string
            if (file.StartsWith(normalizedDirectory, StringComparison.Ordinal))
            {
                foundFiles.Add(file);
            }
        }
        return foundFiles;
    }

    public bool FileExists(string path)
    {
        return _allFiles.ContainsKey(NormalizePath(path));
    }

    public void MoveFile(string source, string destination)
    {
        var normalizedSource = NormalizePath(source);
        var normalizedDestination = NormalizePath(destination);

        if (!_allFiles.ContainsKey(normalizedSource))
        {
            throw new FileNotFoundException("Couldn't move file, source file does not exist: " + source);
        }
        if (_allFiles.ContainsKey(normalizedDestination))
        {
            throw new IOException("Destination exists, could not move file");
        }
        var data = _allFiles[normalizedSource];
        _allFiles.Remove(normalizedSource);
        _allFiles.Add(normalizedDestination, data);
    }

    public void CopyFile(string source, string destination)
    {
        var normalizedSource = NormalizePath(source);
        var normalizedDestination = NormalizePath(destination);

        if (!_allFiles.ContainsKey(normalizedSource))
        {
            throw new FileNotFoundException("Could not copy file, it does not exist: " + source);
        }
        // Overwrite old file
        if (_allFiles.ContainsKey(normalizedDestination))
        {
            _allFiles.Remove(normalizedDestination);
        }
        byte[] originalData = _allFiles[normalizedSource].Data;
        int length = originalData.Length;
        byte[] dataCopy = new byte[length];
        Array.Copy(originalData, dataCopy, length);
        _allFiles.Add(normalizedDestination, new MockFile(dataCopy));
    }

    public void DeleteFile(string path)
    {
        var normalizedPath = NormalizePath(path);
        if (_allFiles.ContainsKey(normalizedPath))
        {
            _allFiles.Remove(normalizedPath);
        }
    }

    public void ReplaceFile(string source, string destination, string backupDestination)
    {
        var normalizedSource = NormalizePath(source);
        var normalizedDestination = NormalizePath(destination);
        var normalizedBackupDestination = NormalizePath(backupDestination);

        if (_allFiles.ContainsKey(normalizedDestination))
        {
            _allFiles[normalizedBackupDestination] = _allFiles[normalizedDestination];
        }
        _allFiles[normalizedDestination] = _allFiles[normalizedSource];
    }

    public IEnumerable<string> GetDirectories(string directoryPath, string? searchPattern = null)
    {
        var normalizedDirectory = NormalizePath(directoryPath).TrimEnd('/');
        var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";

        return _directories
            .Where(dir => dir.Equals(normalizedDirectory, StringComparison.Ordinal)
                          || dir.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();
    }

    public bool DirectoryExists(string directoryPath)
    {
        var normalizedDirectory = NormalizePath(directoryPath);
        return _directories.Contains(normalizedDirectory);
    }

    public void CreateDirectory(string directoryPath)
    {
        var normalizedDirectory = NormalizePath(directoryPath);
        _directories.Add(normalizedDirectory);
    }

    public void DeleteDirectory(string directoryPath)
    {
        var normalizedDirectory = NormalizePath(directoryPath).TrimEnd('/');
        var prefix = normalizedDirectory.Length == 0 ? string.Empty : normalizedDirectory + "/";

        _directories.RemoveWhere(dir => dir.Equals(normalizedDirectory, StringComparison.Ordinal)
                                        || dir.StartsWith(prefix, StringComparison.Ordinal));

        var filesToRemove = _allFiles.Keys
            .Where(file => file.Equals(normalizedDirectory, StringComparison.Ordinal)
                           || file.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var file in filesToRemove)
        {
            _allFiles.Remove(file);
        }
    }

    public Stream OpenRead(string path)
    {
        var normalizedPath = NormalizePath(path);

        if (!_allFiles.ContainsKey(normalizedPath))
        {
            throw new FileNotFoundException("File not found: " + path);
        }
        return _allFiles[normalizedPath].OpenRead();
    }

    public Stream OpenWrite(string path)
    {
        var normalizedPath = NormalizePath(path);

        if (!_allFiles.TryGetValue(normalizedPath, out var file))
        {
            file = new MockFile();
            _allFiles.Add(normalizedPath, file);
        }
        return file.OpenWrite();
    }

    public void WriteAllText(string path, string contents)
    {
        _allFiles[NormalizePath(path)] = new MockFile(Encoding.UTF8.GetBytes(contents));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }
}
