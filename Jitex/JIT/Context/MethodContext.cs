using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Context for method resolution.
    /// </summary>
    public class MethodContext
    {
        /// <summary>
        /// Resolution mode.
        /// </summary>
        public enum ResolveMode
        {
            /// <summary>
            /// MSIL (pre-compile)
            /// </summary>
            IL,

            /// <summary>
            /// Bytecode (pos-compile)
            /// </summary>
            NATIVE
        }

        /// <summary>
        /// Method original which will compiled.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// Body of method to compile.
        /// </summary>
        public MethodBody MethodBody { get; private set; }

        /// <summary>
        /// If method is already resolved
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Byte-code from method (only to ASM mode)
        /// </summary>
        internal byte[]? NativeCode { get; private set; }

        /// <summary>
        /// Resolution mode.
        /// </summary>
        /// <remarks>
        /// IL to MSIL
        /// ASM to byte-code.
        /// </remarks>
        internal ResolveMode Mode => NativeCode == null ? ResolveMode.IL : ResolveMode.NATIVE;

        internal MethodContext(MethodBase method)
        {
            MethodBody = new MethodBody(method);
            Method = method;
        }

        /// <summary>
        /// Resolve method by native code (asm).
        /// </summary>
        /// <param name="nativeCode">ASM to inject.</param>
        public void ResolveNative(IEnumerable<byte> nativeCode)
        {
            NativeCode = nativeCode.ToArray();
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        public void ResolveIL(IEnumerable<byte> il)
        {
            MethodBody = new MethodBody(il.ToArray());
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="maxStack">Stack size to instrucitons.</param>
        public void ResolveIL(IEnumerable<byte> il, uint maxStack)
        {
            MethodBody = new MethodBody(il.ToArray(), maxStack);
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by MethodBody.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public void ResolveBody(MethodBody methodBody)
        {
            MethodBody = methodBody;
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by MethodInfo.
        /// </summary>
        /// <param name="method">Body of new method.</param>
        public void ResolveMethod(MethodInfo method)
        {
            MethodBody = new MethodBody(method);
            IsResolved = true;
        }
    }
}