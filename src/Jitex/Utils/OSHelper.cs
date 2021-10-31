using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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

        public static (IntPtr address, int size) GetModuleBaseAddress(string modulePath)
        {
            IntPtr address;
            int size;

            if (IsWindows)
                (address, size) = GetModuleWindows(modulePath);
            else if (IsLinux)
                (address, size) = GetModuleLinux(modulePath);
            else
                (address, size) = GetModuleOSX(modulePath);

            if (address == default)
                throw new BadImageFormatException($"Base address for module {modulePath} not found!");

            byte b1 = MemoryHelper.Read<byte>(address);
            byte b2 = MemoryHelper.Read<byte>(address, 1);

            //Validate if address start with MZ
            if (b1 != 0x4D || b2 != 0x5A)
                throw new BadImageFormatException();

            return (address, size);
        }

        private static (IntPtr address, int size) GetModuleLinux(string modulePath)
        {
            int size = 0;

            lock (LockSelfMapsLinux)
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

        private static (IntPtr address, int size) GetModuleWindows(string modulePath)
        {
            foreach (ProcessModule pModule in Process.GetCurrentProcess().Modules)
            {
                if (pModule.FileName == modulePath)
                    return (pModule.BaseAddress, pModule.ModuleMemorySize);
            }

            return default;
        }

        private static (IntPtr address, int size) GetModuleOSX(string modulePath)
        {
            //TODO: Get modules from mach_vm_region and proc_regionfilename.
            Process proc = new Process
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

            StringBuilder sb = new StringBuilder();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                sb.AppendLine(line);
                if (!line.EndsWith(Path.GetFileName(modulePath)))
                    continue;

                int middleAddress = line.IndexOf("-");
                int startRangeAddress = line.LastIndexOf(' ', middleAddress) + 1;
                long startAddress = long.Parse(line[startRangeAddress..middleAddress], NumberStyles.HexNumber);

                return (new IntPtr(startAddress), int.MaxValue);
            }

            return default;
        }
    }
}