using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    internal static class ModuleHelper
    {
        private static IDictionary<IntPtr, Module>? _mapAddressToModule;
        private static readonly FieldInfo m_pData;
        private static readonly MethodInfo GetHInstance;

        private static readonly object LoadLock = new();
        private static readonly object LockSelfMapsLinux = new object();

        static ModuleHelper()
        {
            m_pData = Type.GetType("System.Reflection.RuntimeModule")
                .GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);
            GetHInstance = typeof(Marshal).GetMethod("GetHINSTANCE", new[] { typeof(Module) })!;
            LoadMapScopeToHandle();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Initialize()
        {
        }

        private static void AddAssembly(Assembly assembly)
        {
            Module module = assembly.Modules.First();
            IntPtr address = GetAddressFromModule(module);
            _mapAddressToModule!.Add(address, module);
        }

        private static void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            LoadMapScopeToHandle();
            AddAssembly(args.LoadedAssembly);
        }

        public static Module? GetModuleByAddress(IntPtr handle)
        {
            LoadMapScopeToHandle();
            return _mapAddressToModule!.TryGetValue(handle, out Module module) ? module : null;
        }

        public static IntPtr GetAddressFromModule(Module module)
        {
            return (IntPtr)m_pData.GetValue(module);
        }

        private static void LoadMapScopeToHandle()
        {
            lock (LoadLock)
            {
                if (_mapAddressToModule != null)
                    return;

                _mapAddressToModule = new Dictionary<IntPtr, Module>();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AddAssembly(assembly);
                }

                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
            }
        }

        public static IntPtr GetModuleHandle(Module module)
        {
            IntPtr address;

            if (OSHelper.IsWindows)
                address = GetModuleWindows(module);
            else if (OSHelper.IsLinux)
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

                    if (!IsValidModuleHandle(address))
                        continue;

                    return address;
                } while (!sr.EndOfStream);
            }

            return default;
        }

        private static IntPtr GetModuleWindows(Module module)
        {
            if (!OSHelper.IsWindows)
                throw new InvalidOperationException();

            return (IntPtr)GetHInstance.Invoke(null, new object[] { module });
        }

        public static IntPtr GetModuleOSX(string modulePath)
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

                if (!IsValidModuleHandle(address))
                    continue;

                return address;
            }

            return default;
        }

        private static bool IsValidModuleHandle(IntPtr address)
        {
            byte b1 = MemoryHelper.Read<byte>(address);
            byte b2 = MemoryHelper.Read<byte>(address, 1);

            //Validate if address start with MZ
            return b1 == 0x4D && b2 == 0x5A;
        }
    }
}