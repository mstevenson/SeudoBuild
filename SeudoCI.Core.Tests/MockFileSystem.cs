namespace SeudoCI.Core.Tests;

/// <summary>
/// A file stream that isn't closed after reading or writing, and flushes to a byte array.
/// </summary>
public class MockFile
{
    public class MockMemoryStream : System.IO.MemoryStream
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

    private MockMemoryStream _writeStream;

    public MockFile()
    {
        Data = new byte[0];
    }

    public MockFile(byte[] data)
    {
        Data = data;
    }

    public System.IO.Stream OpenRead()
    {
        if (_writeStream != null)
        {
            _writeStream.Flush();
            Data = _writeStream.ToArray();
        }
        var readStream = new MockMemoryStream(Data, OnFlush);
        return readStream;
    }

    public System.IO.Stream OpenWrite()
    {
        if (_writeStream != null && _writeStream.CanWrite)
        {
            throw new System.IO.IOException("Can't write to mock file, the mock file stream is already open");
        }
        else
        {
            _writeStream = new MockMemoryStream(OnFlush);
        }
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
    private readonly Dictionary<string, MockFile> _allFiles = new Dictionary<string, MockFile>();

    public string TemporaryFilesPath => "/temp";

    public string StandardOutputPath { get; } = "/dev/null";

    public string DocumentsPath => "/documents";

    public IEnumerable<string> GetFiles(string directoryPath, string searchPattern = null)
    {
        var foundFiles = new List<string>();
        foreach (var file in _allFiles.Keys)
        {
            // TODO implement search string
            if (file.StartsWith(directoryPath))
            {
                foundFiles.Add(file);
            }
        }
        return foundFiles;
    }

    public bool FileExists(string path)
    {
        return _allFiles.ContainsKey(path);
    }

    public void MoveFile(string source, string destination)
    {
        if (!_allFiles.ContainsKey(source))
        {
            throw new System.IO.FileNotFoundException("Couldn't move file, source file does not exist: " + source);
        }
        if (_allFiles.ContainsKey(destination))
        {
            throw new System.IO.IOException("Destination exists, could not move file");
        }
        var data = _allFiles[source];
        _allFiles.Remove(source);
        _allFiles.Add(destination, data);
    }

    public void CopyFile(string source, string destination)
    {
        if (!_allFiles.ContainsKey(source))
        {
            throw new System.IO.FileNotFoundException("Could not copy file, it does not exist: " + source);
        }
        // Overwrite old file
        if (_allFiles.ContainsKey(destination))
        {
            _allFiles.Remove(destination);
        }
        byte[] originalData = _allFiles[source].Data;
        int length = originalData.Length;
        byte[] dataCopy = new byte[length];
        Array.Copy(originalData, dataCopy, length);
        _allFiles.Add(destination, new MockFile(dataCopy));
    }

    public void DeleteFile(string path)
    {
        if (_allFiles.ContainsKey(path))
        {
            _allFiles.Remove(path);
        }
    }

    public void ReplaceFile(string source, string destination, string backupDestination)
    {
        if (_allFiles.ContainsKey(destination))
        {
            _allFiles[backupDestination] = _allFiles[destination];
        }
        _allFiles[destination] = _allFiles[source];
    }

    public IEnumerable<string> GetDirectories(string directoryPath, string searchPattern = null)
    {
        throw new System.NotImplementedException();
    }

    public bool DirectoryExists(string directoryPath)
    {
        throw new System.NotImplementedException();
    }

    public void CreateDirectory(string directoryPath)
    {
        throw new System.NotImplementedException();
    }

    public void DeleteDirectory(string directoryPath)
    {
        throw new System.NotImplementedException();
    }

    public System.IO.Stream OpenRead(string path)
    {
        if (!_allFiles.ContainsKey(path))
        {
            throw new System.IO.FileNotFoundException("File not found: " + path);
        }
        var file = _allFiles[path].OpenRead();
        return file;
    }

    public System.IO.Stream OpenWrite(string path)
    {
        MockFile file = null;
        if (!_allFiles.TryGetValue(path, out file))
        {
            file = new MockFile();
            _allFiles.Add(path, file);
        }
        return file.OpenWrite();
    }

    public void WriteAllText(string path, string contents)
    {
        throw new System.NotImplementedException();
    }
}