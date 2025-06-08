namespace SeudoCI.Core;

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
                    return Platform.Linux;
                case PlatformID.MacOSX:
                    return Platform.Mac;
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.Win32NT:
                case PlatformID.WinCE:
                    return Platform.Windows;
                case PlatformID.Xbox:
                case PlatformID.Other:
                default:
                    throw new PlatformNotSupportedException("The current platform is not supported by SeudoCI.");
            }
        }
    }
}