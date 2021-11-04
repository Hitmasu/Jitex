using Jitex.JIT.Context;
using Jitex.JIT.CorInfo;
using Jitex.Runtime;
using System;
using System.Reflection;

namespace Jitex.JIT.Handlers
{
    public class MethodCompiled
    {
        /// <summary>
        /// Native code compiled.
        /// </summary>
        public NativeCode NativeCode { get; }

        /// <summary>
        /// Method compiled
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// MethodContext used on MethodResolver.
        /// </summary>
        public MethodContext? Context { get; }

        /// <summary>
        /// CORINFO_METHOD_INFO from method.
        /// </summary>
        public CorInfo.MethodInfo CorMethodInfo { get; set; }

        /// <summary>
        /// Result from JIT.
        /// </summary>
        public CorJitResult Result { get; set; }

        internal MethodCompiled(MethodBase method, MethodContext? context, CorInfo.MethodInfo corMethodInfo, CorJitResult result, IntPtr nativeAddress, int size)
        {
            NativeCode = new NativeCode(nativeAddress,size);
            Method = method;
            Context = context;
            Result = result;
            CorMethodInfo = corMethodInfo;
        }
    }
}
