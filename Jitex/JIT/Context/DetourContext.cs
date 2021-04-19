using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Runtime;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    public class DetourContext
    {
        /// <summary>
        /// Original Native Code
        /// </summary>
        private byte[]? _originalNativeCode;

        private readonly byte[] _trampolineCode;

        public bool IsDetoured { get; private set; }

        /// <summary>
        /// Address of Native Code
        /// </summary>
        internal IntPtr MethodAddress { get; set; }
        
        internal DetourContext(IntPtr address)
        {
            _trampolineCode = Trampoline.GetTrampoline(address);
        }

        internal DetourContext(MethodBase methodInterceptor) : this(RuntimeMethodCache.GetNativeAddress(methodInterceptor)){}

        internal void WriteDetour()
        {
            if (_originalNativeCode == null)
            {
                _originalNativeCode = new byte[Trampoline.Size];

                //Create backup of original instructions
                Marshal.Copy(MethodAddress, _originalNativeCode, 0, Trampoline.Size);
            }

            //Write trampoline
            Marshal.Copy(_trampolineCode!, 0, MethodAddress, _trampolineCode!.Length);
            IsDetoured = true;
        }

        internal void RemoveDetour()
        {
            if (!IsDetoured)
                throw new InvalidOperationException("Method was not detoured!");

            Marshal.Copy(_originalNativeCode!, 0, MethodAddress, _originalNativeCode!.Length);
            IsDetoured = false;
        }
    }
}