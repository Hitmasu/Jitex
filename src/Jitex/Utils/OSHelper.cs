using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.Utils
{
    public static class OSHelper
    {
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsArm64 => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;


        public static bool IsHardenedRuntime => IsOSX && IsArm64;

        public static bool IsPosix => !IsWindows;
    }
}