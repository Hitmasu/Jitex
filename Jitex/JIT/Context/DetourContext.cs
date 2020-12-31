using System;
using System.Runtime.InteropServices;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Detour mode
    /// </summary>
    public enum DetourMode
    {
        /// <summary>
        /// Replace address method by another address.
        /// </summary>
        DirectCall,

        /// <summary>
        /// Create a trampoline on method address to another address.
        /// </summary>
        Trampoline
    }

    public class DetourContext
    {
        public IntPtr Address { get; }
        public int Size { get; }
        public byte[] NativeCode { get; }

        public bool IsDetoured { get; private set; }

        /// <summary>
        /// Address of Native Code
        /// </summary>
        internal IntPtr NativeAddress { get; set; }

        /// <summary>
        /// Original Native Code (Only Trampoline Mode)
        /// </summary>
        private byte[] OriginalNativeCode { get; }

        public DetourMode Mode { get; }

        internal DetourContext(byte[] nativeCode)
        {
            NativeCode = nativeCode;
            Mode = DetourMode.Trampoline;
            OriginalNativeCode = new byte[Trampoline.Size];
        }

        internal DetourContext(IntPtr address, int size)
        {
            Address = address;
            Size = size;
            Mode = DetourMode.DirectCall;
        }

        protected DetourContext()
        {
        }

        internal void WriteDetour()
        {
            if (Mode == DetourMode.DirectCall)
                throw new InvalidOperationException("Detour as DirectCAll cannot be detoured!");

            Marshal.Copy(NativeAddress, OriginalNativeCode, 0, Trampoline.Size);
            Marshal.Copy(NativeCode!, 0, NativeAddress, NativeCode!.Length);
            IsDetoured = true;
        }

        internal void RemoveDetour()
        {
            if (!IsDetoured)
                throw new InvalidOperationException("Method was not detoured!");

            if (Mode == DetourMode.DirectCall)
                throw new InvalidOperationException("Detour as DirectCall cannot be removed!");

            Marshal.Copy(OriginalNativeCode!, 0, NativeAddress, OriginalNativeCode!.Length);
            IsDetoured = false;
        }
    }
}