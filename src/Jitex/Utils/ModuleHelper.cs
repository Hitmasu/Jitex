using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using dnlib.DotNet;

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
    }
}