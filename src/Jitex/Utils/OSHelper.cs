using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    internal static class OSHelper
    {
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsPosix => !IsWindows;

        public static (IntPtr address, int size) GetModuleBaseAddress(string modulePath)
        {
            if (IsWindows | IsOSX)
            {
                foreach (ProcessModule pModule in Process.GetCurrentProcess().Modules)
                {
                    if (pModule.FileName == modulePath)
                        return (pModule.BaseAddress, pModule.ModuleMemorySize);
                }

                return default;
            }

            if (IsLinux)
            {
                using FileStream fs = File.OpenRead("/proc/self/maps");
                using StreamReader sr = new StreamReader(fs);

                do
                {
                    string line = sr.ReadLine()!;

                    if (!line.EndsWith(modulePath))
                        continue;

                    int separator = line.IndexOf("-", StringComparison.Ordinal);

                    //TODO: Implement Span in future...
                    long startAddress = long.Parse(line[..separator], NumberStyles.HexNumber);

                    int size = 0;
                    foreach (ProcessModule pModule in Process.GetCurrentProcess().Modules)
                    {
                        if (pModule.FileName == modulePath)
                            size = pModule.ModuleMemorySize;
                    }

                    return (new IntPtr(startAddress), size);
                } while (!sr.EndOfStream);
            }

            return default;
        }
    }
}