using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Jitex.Utils
{
    internal static class OSHelper
    {
        private static readonly object LockSelfMapsLinux = new object();

        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsPosix => !IsWindows;

        public static IntPtr GetModuleHandle(Module module)
        {
            IntPtr address;

            if (IsWindows)
                address = GetModuleWindows(module);
            else if (IsLinux)
                address = GetModuleLinux(module.FullyQualifiedName);
            else
                address = GetModuleOSX(module.FullyQualifiedName);

            if (address == default)
                throw new BadImageFormatException($"Base address for module {module.FullyQualifiedName} not found!");

            return address;
        }

        private static IntPtr GetModuleLinux(string modulePath)
        {
            lock (LockSelfMapsLinux)
            {
                using FileStream fs = File.OpenRead("/proc/self/maps");
                using StreamReader sr = new(fs);

                do
                {
                    string line = sr.ReadLine()!;
                    if (!line.EndsWith(modulePath))
                        continue;

                    int separator = line.IndexOf("-", StringComparison.Ordinal);

                    //TODO: Implement Span in future...
                    IntPtr address = new(long.Parse(line[..separator], NumberStyles.HexNumber));

                    if (!ValidateModuleHandle(address))
                        continue;

                    return address;
                } while (!sr.EndOfStream);
            }

            return default;
        }

        private static IntPtr GetModuleWindows(Module module) => ModuleHelper.GetModuleHandle(module);

        private static IntPtr GetModuleOSX(string modulePath)
        {
            //TODO: Get modules from mach_vm_region and proc_regionfilename.
            Process proc = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "vmmap",
                    Arguments = Process.GetCurrentProcess().Id.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                string? line = proc.StandardOutput.ReadLine();

                if (string.IsNullOrEmpty(line))
                    break;

                if (!line.EndsWith(Path.GetFileName(modulePath)))
                    continue;

                int middleAddress = line.IndexOf("-");
                int startRangeAddress = line.LastIndexOf(' ', middleAddress) + 1;
                IntPtr address = new(long.Parse(line[startRangeAddress..middleAddress], NumberStyles.HexNumber));

                if (!ValidateModuleHandle(address))
                    continue;

                return address;
            }

            return default;
        }

        private static bool ValidateModuleHandle(IntPtr address)
        {
            byte b1 = MemoryHelper.Read<byte>(address);
            byte b2 = MemoryHelper.Read<byte>(address, 1);

            //Validate if address start with MZ
            return b1 == 0x4D && b2 == 0x5A;
        }
    }
}