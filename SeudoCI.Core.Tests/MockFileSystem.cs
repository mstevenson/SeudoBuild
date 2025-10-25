using System.Text;
using System.Text.RegularExpressions;

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

    private MockMemoryStream _writeStream;

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
        _writeStream.Flush();
        Data = _writeStream.ToArray();
        var readStream = new MockMemoryStream(Data, OnFlush);
        return readStream;
    }

    public Stream OpenWrite()
    {
        if (_writeStream.CanWrite)
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
    private readonly Dictionary<string, MockFile> _allFiles = new Dictionary<string, MockFile>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "/" };

    public string TemporaryFilesPath => "/temp";

    public string StandardOutputPath { get; } = "/dev/null";

    public string DocumentsPath => "/documents";

    public IEnumerable<string> GetFiles(string directoryPath, string? searchPattern = null)
    {
        var normalizedDirectory = NormalizeDirectoryPath(directoryPath);
        var pattern = string.IsNullOrEmpty(searchPattern) ? "*" : searchPattern;
        var matches = new List<string>();

        foreach (var file in _allFiles.Keys)
        {
            if (!string.Equals(GetDirectoryNameForFile(file), normalizedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (MatchesPattern(GetFileName(file), pattern))
            {
                matches.Add(file);
            }
        }

        matches.Sort(StringComparer.OrdinalIgnoreCase);
        return matches;
    }

    public bool FileExists(string path)
    {
        path = NormalizePath(path);
        return _allFiles.ContainsKey(path);
    }

    public void MoveFile(string source, string destination)
    {
        source = NormalizePath(source);
        destination = NormalizePath(destination);

        if (!_allFiles.ContainsKey(source))
        {
            throw new FileNotFoundException("Couldn't move file, source file does not exist: " + source);
        }
        if (_allFiles.ContainsKey(destination))
        {
            throw new IOException("Destination exists, could not move file");
        }
        var data = _allFiles[source];
        _allFiles.Remove(source);
        _allFiles.Add(destination, data);
    }

    public void CopyFile(string source, string destination)
    {
        source = NormalizePath(source);
        destination = NormalizePath(destination);

        if (!_allFiles.ContainsKey(source))
        {
            throw new FileNotFoundException("Could not copy file, it does not exist: " + source);
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
        path = NormalizePath(path);

        if (_allFiles.ContainsKey(path))
        {
            _allFiles.Remove(path);
        }
    }

    public void ReplaceFile(string source, string destination, string backupDestination)
    {
        source = NormalizePath(source);
        destination = NormalizePath(destination);
        backupDestination = NormalizePath(backupDestination);

        if (_allFiles.ContainsKey(destination))
        {
            _allFiles[backupDestination] = _allFiles[destination];
        }
        _allFiles[destination] = _allFiles[source];
    }

    public IEnumerable<string> GetDirectories(string directoryPath, string? searchPattern = null)
    {
        var normalizedDirectory = NormalizeDirectoryPath(directoryPath);
        var pattern = string.IsNullOrEmpty(searchPattern) ? "*" : searchPattern;
        var matches = new List<string>();

        foreach (var directory in _directories)
        {
            if (string.Equals(directory, normalizedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!IsDirectChildDirectory(directory, normalizedDirectory))
            {
                continue;
            }

            if (MatchesPattern(GetFileName(directory), pattern))
            {
                matches.Add(directory);
            }
        }

        matches.Sort(StringComparer.OrdinalIgnoreCase);
        return matches;
    }

    public bool DirectoryExists(string directoryPath)
    {
        var normalized = NormalizeDirectoryPath(directoryPath);
        return _directories.Contains(normalized);
    }

    public void CreateDirectory(string directoryPath)
    {
        var normalized = NormalizeDirectoryPath(directoryPath);
        var isAbsolute = IsAbsolute(normalized);
        var segments = GetSegments(normalized);

        if (segments.Count == 0)
        {
            _directories.Add(isAbsolute ? "/" : string.Empty);
            return;
        }

        var current = isAbsolute ? "/" : string.Empty;
        foreach (var segment in segments)
        {
            current = AppendSegment(current, segment, isAbsolute);
            if (!string.IsNullOrEmpty(current))
            {
                _directories.Add(current);
            }
        }
    }

    public void DeleteDirectory(string directoryPath)
    {
        var normalized = NormalizeDirectoryPath(directoryPath);

        var directoriesToRemove = _directories
            .Where(dir => IsSameOrSubdirectory(dir, normalized))
            .ToList();

        foreach (var dir in directoriesToRemove)
        {
            _directories.Remove(dir);
        }

        var filesToRemove = _allFiles.Keys
            .Where(file => IsFileInDirectory(file, normalized))
            .ToList();

        foreach (var file in filesToRemove)
        {
            _allFiles.Remove(file);
        }
    }

    public Stream OpenRead(string path)
    {
        path = NormalizePath(path);

        if (!_allFiles.ContainsKey(path))
        {
            throw new FileNotFoundException("File not found: " + path);
        }
        var file = _allFiles[path].OpenRead();
        return file;
    }

    public Stream OpenWrite(string path)
    {
        path = NormalizePath(path);

        if (!_allFiles.TryGetValue(path, out var file))
        {
            file = new MockFile();
            _allFiles.Add(path, file);
        }
        return file.OpenWrite();
    }

    public void WriteAllText(string path, string contents)
    {
        path = NormalizePath(path);

        var data = Encoding.UTF8.GetBytes(contents ?? string.Empty);
        _allFiles[path] = new MockFile(data);
    }

    private static string NormalizePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var normalized = path.Replace('\\', '/');

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            var isDriveRoot = normalized.Length == 3 && normalized[1] == ':' && normalized[2] == '/';
            if (!isDriveRoot)
            {
                normalized = normalized.TrimEnd('/');
            }
        }

        return normalized;
    }

    private static string NormalizeDirectoryPath(string path)
    {
        var normalized = NormalizePath(path);
        return normalized.Length == 0 ? string.Empty : normalized;
    }

    private static string GetDirectoryNameForFile(string path)
    {
        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        if (lastSlash < 0)
        {
            return string.Empty;
        }

        return lastSlash == 0 ? "/" : normalized.Substring(0, lastSlash);
    }

    private static string GetFileName(string path)
    {
        var normalized = NormalizePath(path);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash < 0 ? normalized : normalized[(lastSlash + 1)..];
    }

    private static bool MatchesPattern(string input, string pattern)
    {
        if (pattern == "*")
        {
            return true;
        }

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }

    private static bool IsAbsolute(string path) => path.StartsWith('/');

    private static IReadOnlyList<string> GetSegments(string path)
    {
        if (path == "/")
        {
            return Array.Empty<string>();
        }

        return path.Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string AppendSegment(string current, string segment, bool isAbsolute)
    {
        if (string.IsNullOrEmpty(current))
        {
            return isAbsolute ? "/" + segment : segment;
        }

        if (current == "/")
        {
            return "/" + segment;
        }

        return current + "/" + segment;
    }

    private static bool IsDirectChildDirectory(string candidate, string parent)
    {
        if (!IsSameOrSubdirectory(candidate, parent))
        {
            return false;
        }

        if (string.Equals(candidate, parent, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var candidateSegments = GetSegments(candidate);
        var parentSegments = GetSegments(parent);
        return candidateSegments.Count == parentSegments.Count + 1;
    }

    private static bool IsSameOrSubdirectory(string candidate, string parent)
    {
        candidate = NormalizeDirectoryPath(candidate);
        parent = NormalizeDirectoryPath(parent);

        if (string.Equals(parent, string.Empty, StringComparison.Ordinal))
        {
            return !candidate.StartsWith('/');
        }

        if (parent == "/")
        {
            return candidate.StartsWith('/');
        }

        if (!candidate.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return candidate.Length == parent.Length || candidate[parent.Length] == '/';
    }

    private static bool IsFileInDirectory(string filePath, string directory)
    {
        var parent = GetDirectoryNameForFile(filePath);
        if (string.IsNullOrEmpty(directory))
        {
            return string.IsNullOrEmpty(parent);
        }

        return string.Equals(parent, directory, StringComparison.OrdinalIgnoreCase)
               || (!string.IsNullOrEmpty(parent) && IsSameOrSubdirectory(parent, directory));
    }
}
