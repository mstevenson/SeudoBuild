using System;
using UnityBuildServer;

namespace UnityBuildServerConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var workspace = Workspace.Create ("~/Desktop/UnityBuildServer/UnityBuildServerWorkspace");

            var repository = new Repository
            {
                URL = "",
                User = "test",
                Password = "test",
                Branch = ""
            };
        }
    }
}
