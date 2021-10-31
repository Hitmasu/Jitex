using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Utils.Comparer;

namespace Jitex.Utils
{
    internal static class AppModules
    {
        private static IDictionary<IntPtr, Module>? _mapScopeToHandle;
        private static readonly FieldInfo m_pData;
        private static readonly object LoadLock = new object();

        static AppModules()
        {
            m_pData = Type.GetType("System.Reflection.RuntimeModule").GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);
            LoadMapScopeToHandle();
        }

        private static void AddAssembly(Assembly assembly)
        {
            Module module = assembly.Modules.First();
            IntPtr scope = GetAddressFromModule(module);
            _mapScopeToHandle!.Add(scope, module);
        }

        private static void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            LoadMapScopeToHandle();
            AddAssembly(args.LoadedAssembly);
        }

        public static Module? GetModuleByHandle(IntPtr handle)
        {
            LoadMapScopeToHandle();
            return _mapScopeToHandle!.TryGetValue(handle, out Module module) ? module : null;
        }

        public static IntPtr GetAddressFromModule(Module module)
        {
            return (IntPtr)m_pData.GetValue(module);
        }

        private static void LoadMapScopeToHandle()
        {
            lock (LoadLock)
            {
                if (_mapScopeToHandle != null)
                    return;

                _mapScopeToHandle = new Dictionary<IntPtr, Module>();

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    AddAssembly(assembly);
                }

                AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
            }
        }
    }
}