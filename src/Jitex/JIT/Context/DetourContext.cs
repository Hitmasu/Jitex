using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Context of a detoured method.
    /// </summary>
    public class DetourContext
    {
        /// <summary>
        /// Original Native Code
        /// </summary>
        private byte[]? _originalNativeCode;

        /// <summary>
        /// Trampoline code
        /// </summary>
        private readonly byte[] _trampolineCode;

        /// <summary>
        /// If context is already detoured.
        /// </summary>
        public bool IsEnabled {get; set; }

        /// <summary>
        /// Address of Native Code
        /// </summary>
        internal IntPtr MethodAddress { get; set; }

        internal DetourContext(IntPtr address)
        {
            _trampolineCode = MemoryHelper.GetTrampoline(address);
        }

        internal DetourContext(MethodBase methodInterceptor) : this(MethodHelper.GetNativeAddress(methodInterceptor))
        {

        }

        /// <summary>
        /// Enable detour on method
        /// </summary>
        /// <returns>True if was success otherwise false if already enabled.</returns>
        public bool Enable()
        {
            if (IsEnabled)
                return false;

            if (_originalNativeCode == null)
            {
                _originalNativeCode = new byte[MemoryHelper.Size];

                //Create backup of original instructions
                Marshal.Copy(MethodAddress, _originalNativeCode, 0, MemoryHelper.Size);
            }

            //Write trampoline
            Marshal.Copy(_trampolineCode!, 0, MethodAddress, _trampolineCode!.Length);
            IsEnabled = true;
            return true;
        }

        /// <summary>
        /// Disable detour on method
        /// </summary>
        /// <returns>True if was success otherwise false if not enabled.</returns>
        public bool Disable()
        {
            if(!IsEnabled)
                return false;

            Marshal.Copy(_originalNativeCode!, 0, MethodAddress, _originalNativeCode!.Length);
            IsEnabled = false;
            return true;
        }
    }
}