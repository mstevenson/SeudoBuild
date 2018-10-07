using System;
using System.IO;

namespace SeudoBuild.Pipeline
{
    public static class PlatformUtils
    {
        public static Platform RunningPlatform
        {
            get
            {
                // macOS is incorrectly detected as Unix. The solution is to check
                // for the presence of macOS root folders.
                // http://stackoverflow.com/questions/10138040/how-to-detect-properly-windows-linux-mac-operating-systems
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Unix:
                        if (Directory.Exists("/Applications")
                            & Directory.Exists("/System")
                            & Directory.Exists("/Users")
                            & Directory.Exists("/Volumes"))
                            return Platform.Mac;
                        else
                            return Platform.Linux;
                    case PlatformID.MacOSX:
                        return Platform.Mac;

                    default:
                        return Platform.Windows;
                }
            }
        }
    }
}