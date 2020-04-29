using System.Reflection;
using MethodBody = Jitex.Builder.MethodBody;

namespace Jitex.JIT
{
    public class ReplaceInfo
    {
        /// <summary>
        ///     Body of method.
        /// </summary>
        public MethodBody MethodBody { get; }

        /// <summary>
        ///     Byte-code from method (Only mode ASM)
        /// </summary>
        public byte[] ByteCode { get; }

        /// <summary>
        ///     Replace mode - IL to MSIL and ASM to ByteCode.
        /// </summary>
        public ReplaceMode Mode => ByteCode == null ? ReplaceMode.IL : ReplaceMode.ASM;

        public enum ReplaceMode
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

        /// <summary>
        ///     Create data to inject a byte-code (ASM mode).
        /// </summary>
        /// <param name="byteCode">Bytecode</param>
        public ReplaceInfo(byte[] byteCode)
        {
            ByteCode = byteCode;
        }

        /// <summary>
        ///     Create data to inject MSIL.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public ReplaceInfo(MethodBody methodBody)
        {
            MethodBody = methodBody;
        }

        /// <summary>
        ///     Create data to inject MSIL from already method exists.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public ReplaceInfo(MethodInfo method)
        {
            MethodBody = new MethodBody(method);
        }
    }
}