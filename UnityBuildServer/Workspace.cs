using System.IO;

namespace UnityBuildServer
{
    public class Workspace
    {
        public string Path { get; private set; }

        public static Workspace Create(string path)
        {
            var ws = new Workspace();
            ws.Path = path;

            Directory.CreateDirectory(ws.Path);

            return ws;
        }
    }
}
