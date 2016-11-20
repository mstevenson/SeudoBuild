using System;
using System.Collections.Generic;

namespace SeudoBuild.Core.Tests
{
    /// <summary>
    /// A file stream that isn't closed after reading or writing, and flushes to a byte array.
    /// </summary>
    public class MockFile
    {
        public class MockMemoryStream : System.IO.MemoryStream
        {
            System.Action<byte[]> flushCallback;

            public MockMemoryStream(System.Action<byte[]> flushCallback) : base()
            {
                this.flushCallback = flushCallback;
            }

            public MockMemoryStream(byte[] buffer, System.Action<byte[]> flushCallback) : base(buffer)
            {
                this.flushCallback = flushCallback;
            }

            public override void Flush()
            {
                base.Flush();
                flushCallback(base.ToArray());
            }

            protected override void Dispose(bool disposing)
            {
                Flush();
                base.Dispose(disposing);
            }
        }

        public byte[] Data { get; private set; }

        MockMemoryStream writeStream;

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
            if (writeStream != null)
            {
                writeStream.Flush();
                Data = writeStream.ToArray();
            }
            var readStream = new MockMemoryStream(Data, OnFlush);
            return readStream;
        }

        public System.IO.Stream OpenWrite()
        {
            if (writeStream != null && writeStream.CanWrite)
            {
                throw new System.IO.IOException("Can't write to mock file, the mock file stream is already open");
            }
            else
            {
                writeStream = new MockMemoryStream(OnFlush);
            }
            return writeStream;
        }

        void OnFlush(byte[] bytes)
        {
            this.Data = bytes;
        }
    }

    /// <summary>
    /// An imaginary file system that only exists in memory.
    /// </summary>
    public class MockFileSystem : IFileSystem
    {
        public Dictionary<string, MockFile> allFiles = new Dictionary<string, MockFile>();

        public string TemporaryFilesPath
        {
            get
            {
                return "/temp";
            }
        }

        public string DocumentsPath
        {
            get
            {
                return "/documents";
            }
        }

        public List<string> GetFiles(string directoryPath, string searchPattern = null)
        {
            List<string> foundFiles = new List<string>();
            foreach (string file in allFiles.Keys)
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
            return allFiles.ContainsKey(path);
        }

        public void MoveFile(string source, string destination)
        {
            if (!allFiles.ContainsKey(source))
            {
                throw new System.IO.FileNotFoundException("Couldn't move file, source file does not exist: " + source);
            }
            if (allFiles.ContainsKey(destination))
            {
                throw new System.IO.IOException("Destination exists, could not move file");
            }
            var data = allFiles[source];
            allFiles.Remove(source);
            allFiles.Add(destination, data);
        }

        public void CopyFile(string source, string destination)
        {
            if (!allFiles.ContainsKey(source))
            {
                throw new System.IO.FileNotFoundException("Could not copy file, it does not exist: " + source);
            }
            // Overwrite old file
            if (allFiles.ContainsKey(destination))
            {
                allFiles.Remove(destination);
            }
            byte[] originalData = allFiles[source].Data;
            int length = originalData.Length;
            byte[] dataCopy = new byte[length];
            Array.Copy(originalData, dataCopy, length);
            allFiles.Add(destination, new MockFile(dataCopy));
        }

        public void DeleteFile(string path)
        {
            if (allFiles.ContainsKey(path))
            {
                allFiles.Remove(path);
            }
        }

        public void ReplaceFile(string source, string destination, string backupDestination)
        {
            if (allFiles.ContainsKey(destination))
            {
                allFiles[backupDestination] = allFiles[destination];
            }
            allFiles[destination] = allFiles[source];
        }

        public List<string> GetDirectories(string directoryPath, string searchPattern = null)
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
            if (!allFiles.ContainsKey(path))
            {
                throw new System.IO.FileNotFoundException("File not found: " + path);
            }
            var file = allFiles[path].OpenRead();
            return file;
        }

        public System.IO.Stream OpenWrite(string path)
        {
            MockFile file = null;
            if (!allFiles.TryGetValue(path, out file))
            {
                file = new MockFile();
                allFiles.Add(path, file);
            }
            return file.OpenWrite();
        }
    }
}