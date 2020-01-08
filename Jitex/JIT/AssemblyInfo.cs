using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Jitex.JIT
{
    class AssemblyInfo
    {
        public AssemblyInfo(Assembly assembly)
        {
            Assembly = assembly;

            IsJitOptmizedEnabled = assembly == null && IsJitOptimizedEnabled(assembly);
        }

        /// <summary>
        /// Detect if jit optimization is enabled in Asseembly.
        /// </summary>
        /// <param name="assembly">Assembly to check.</param>
        /// <returns>True is Jit Optimization is ENABLED</returns>
        /// <seealso>https://dave-black.blogspot.com/2011/12/how-to-tell-if-assembly-is-debug-or.html</seealso>
        private static bool IsJitOptimizedEnabled(Assembly assembly)
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
            if (attributes.Length == 0)
                return true;

            if (attributes[0] is DebuggableAttribute debuggableAttribute)
                return debuggableAttribute.IsJITOptimizerDisabled;

            return true;
        }

        public Assembly Assembly { get; }
        public bool IsJitOptmizedEnabled { get; }
    }
}
