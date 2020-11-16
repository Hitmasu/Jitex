using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Utils.Comparer;

namespace Jitex.Utils
{
    internal static class AppModules
    {
        private static readonly IDictionary<IntPtr, Module> MapScopeToHandle = new Dictionary<IntPtr, Module>(IntPtrEqualityComparer.Instance);

        private static readonly FieldInfo m_pData;
        private static object _lock = new object();

        static AppModules()
        {
            m_pData = Type.GetType("System.Reflection.RuntimeModule").GetField("m_pData", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(assembly);
            }

            AppDomain.CurrentDomain.AssemblyLoad += CurrentDomainOnAssemblyLoad;
        }

        private static void AddAssembly(Assembly assembly)
        {
            Module module = assembly.Modules.First();
            IntPtr scope = GetAddressFromModule(module);
            MapScopeToHandle.Add(scope, module);
        }

        private static void CurrentDomainOnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            AddAssembly(args.LoadedAssembly);
        }

        public static Module? GetModuleByAddress(IntPtr scope)
        {
            return MapScopeToHandle.TryGetValue(scope, out Module module) ? module : null;
        }

        public static IntPtr GetAddressFromModule(Module module)
        {
            return (IntPtr)m_pData.GetValue(module);
        }
    }
}