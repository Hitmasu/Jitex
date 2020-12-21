using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Utils;
using MethodBody = Jitex.Builder.Method.MethodBody;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Context for method resolution.
    /// </summary>
    public class MethodContext
    {
        private MethodBody? _body;

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
        /// Source from call
        /// </summary>
        public MethodBase? Source { get; }

        /// <summary>
        /// Method original which will compiled.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// Body of method to compile.
        /// </summary>
        public MethodBody Body
        {
            get => _body ??= new MethodBody(Method);
            private set => _body = value;
        }

        /// <summary>
        /// If method is already resolved
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Byte-code from method (only to ASM mode)
        /// </summary>
        internal byte[]? NativeCode { get; private set; }

        internal bool IsDetour { get; private set; }

        /// <summary>
        /// Resolution mode.
        /// </summary>
        /// <remarks>
        /// IL to MSIL
        /// ASM to byte-code.
        /// </remarks>
        internal ResolveMode Mode => NativeCode == null ? ResolveMode.IL : ResolveMode.NATIVE;

        internal MethodContext(MethodBase method, MethodBase? source)
        {
            Method = method;
            Source = source;
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
            Body = new MethodBody(il.ToArray());
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="maxStack">Stack size to instrucitons.</param>
        public void ResolveIL(IEnumerable<byte> il, uint maxStack)
        {
            Body = new MethodBody(il.ToArray(), maxStack);
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by MethodBody.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public void ResolveBody(MethodBody methodBody)
        {
            Body = methodBody;
            IsResolved = true;
        }

        /// <summary>
        /// Resolve method by MethodInfo.
        /// </summary>
        /// <param name="method">Body of new method.</param>
        public void ResolveMethod(MethodInfo method)
        {
            Body = new MethodBody(method);
            IsResolved = true;
        }

        /// <summary>
        /// Detour current method.
        /// </summary>
        /// <param name="method"></param>
        public void ResolveDetour(MethodInfo method)
        {
            NativeCode = DetourHelper.CreateDetour(method);
            IsResolved = true;
            IsDetour = true;
        }

        public void ResolveDetour(IntPtr address)
        {
            NativeCode = DetourHelper.CreateDetour(address);
            IsResolved = true;
            IsDetour = true;
        }

        public void ResolveDetour(Delegate del)
        {
            NativeCode = DetourHelper.CreateDetour(del);
            IsResolved = true;
            IsDetour = true;
        }

        public void ResolveDetour<T>(T del) where T : Delegate
        {
            NativeCode = DetourHelper.CreateDetour(del);
            IsResolved = true;
            IsDetour = true;
        }
    }
}