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
            ASM
        }

        /// <summary>
        /// Method who will compiled.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// Body of method.
        /// </summary>
        internal MethodBody MethodBody { get; private set; }

        /// <summary>
        /// If method is already resolved
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Byte-code from method (only to ASM mode)
        /// </summary>
        internal byte[] ByteCode { get; private set; }

        /// <summary>
        /// Resolution mode.
        /// </summary>
        /// <remarks>
        /// IL to MSIL
        /// ASM to byte-code.
        /// </remarks>
        internal ResolveMode Mode => ByteCode == null ? ResolveMode.IL : ResolveMode.ASM;

        internal MethodContext(MethodBase method)
        {
            MethodBody = new MethodBody(method);
            Method = method;
        }

        /// <summary>
        /// Resolve method by byte-code.
        /// </summary>
        /// <param name="byteCode">Bytecode to inject.</param>
        public void ResolveByteCode(byte[] byteCode)
        {
            ByteCode = byteCode;
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