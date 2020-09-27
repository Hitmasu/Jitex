using System.Reflection;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.JIT
{
    public class CompileContext
    {
        /// <summary>
        /// Method who will compiled.
        /// </summary>
        public MethodBase Method { get; }
        
        /// <summary>
        ///     Body of method.
        /// </summary>
        internal MethodBody MethodBody { get; private set; }

        public bool IsResolved { get; private set; }

        /// <summary>
        ///     Byte-code from method (Only mode ASM)
        /// </summary>
        internal byte[] ByteCode { get; private set; }

        /// <summary>
        ///     Replace mode - IL to MSIL and ASM to ByteCode.
        /// </summary>
        internal ResolveMode Mode => ByteCode == null ? ResolveMode.IL : ResolveMode.ASM;

        public enum ResolveMode
        {
            /// <summary>
            ///     MSIL (pre-compile)
            /// </summary>
            IL,

            /// <summary>
            ///     Bytecode (pos-compile)
            /// </summary>
            ASM
        }

        internal CompileContext(MethodBase method)
        {
            MethodBody = new MethodBody(method);
            Method = method;
        }

        /// <summary>
        ///     Create data to inject a byte-code (ASM mode).
        /// </summary>
        /// <param name="byteCode">Bytecode</param>
        public void ResolveByteCode(byte[] byteCode)
        {
            ByteCode = byteCode;
            IsResolved = true;
        }

        /// <summary>
        ///     Create data to inject MSIL.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public void ResolveBody(MethodBody methodBody)
        {
            MethodBody = methodBody;
            IsResolved = true;
        }

        /// <summary>
        ///     Create data to inject MSIL from already method exists.
        /// </summary>
        /// <param name="method">Body of new method.</param>
        public void ResolveMethod(MethodInfo method)
        {
            MethodBody = new MethodBody(method);
            IsResolved = true;
        }
    }
}