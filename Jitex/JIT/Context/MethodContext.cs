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
            Native,

            /// <summary>
            /// Detour
            /// </summary>
            Detour
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

        public DetourContext DetourContext { get; private set; }

        /// <summary>
        /// Resolution mode.
        /// </summary>
        internal ResolveMode Mode { get; private set; }

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
            Mode = ResolveMode.Native;
        }

        /// <summary>
        /// Resolve method by IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="maxStack">Stack size to instrucitons.</param>
        public void ResolveIL(IEnumerable<byte> il, uint maxStack = 8)
        {
            Body = new MethodBody(il.ToArray(), maxStack);
            IsResolved = true;
            Mode = ResolveMode.IL;
        }

        /// <summary>
        /// Resolve method by MethodBase.
        /// </summary>
        /// <param name="method">Body of new method.</param>
        public void ResolveMethod(MethodBase method)
        {
            ResolveBody(new MethodBody(method));
        }

        /// <summary>
        /// Resolve method by MethodBody.
        /// </summary>
        /// <param name="methodBody">Body of new method.</param>
        public void ResolveBody(MethodBody methodBody)
        {
            Body = methodBody;
            IsResolved = true;
            Mode = ResolveMode.IL;
        }

        /// <summary>
        /// Detour to another method.
        /// </summary>
        /// <param name="method"></param>
        public void ResolveDetour(MethodInfo method, DetourMode mode = DetourMode.Trampoline)
        {
            ResolveDetour(method as MethodBase, mode);
        }

        /// <summary>
        /// Detour to a Delegate
        /// </summary>
        /// <param name="del"></param>
        public void ResolveDetour(Delegate del, DetourMode mode = DetourMode.Trampoline)
        {
            ResolveDetour<Delegate>(del, mode);
        }

        /// <summary>
        /// Detour to a Delegate
        /// </summary>
        /// <param name="del"></param>
        /// <typeparam name="T"></typeparam>
        public void ResolveDetour<T>(T del, DetourMode mode = DetourMode.Trampoline) where T : Delegate
        {
            ResolveDetour(del.Method, mode);
        }

        /// <summary>
        /// Detour to a address
        /// </summary>
        /// <param name="address"></param>
        public void ResolveDetour(IntPtr address, int size = 0, DetourMode mode = DetourMode.Trampoline)
        {
            if (mode == DetourMode.Trampoline)
            {
                byte[] nativeCode = DetourHelper.CreateDetour(address);
                DetourContext = new DetourContext(nativeCode);
            }
            else
            {
                DetourContext = new DetourContext(address, size);
            }

            IsResolved = true;
            Mode = ResolveMode.Detour;
        }

        /// <summary>
        /// Detour to another method.
        /// </summary>
        /// <param name="method"></param>
        public void ResolveDetour(MethodBase method, DetourMode mode = DetourMode.Trampoline)
        {
            if (mode == DetourMode.Trampoline)
            {
                byte[] nativeCode = DetourHelper.CreateDetour(method);
                DetourContext = new DetourContext(nativeCode);
            }
            else
            {
                IntPtr address = MethodHelper.GetMethodAddress(method);
                DetourContext = new DetourContext(address, 0);
            }

            IsResolved = true;
            Mode = ResolveMode.Detour;
        }
    }
}