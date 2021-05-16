using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    internal static class OSHelper
    {
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsPosix => !IsWindows;
    }
}