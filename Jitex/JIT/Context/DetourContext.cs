using System;

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
        public IntPtr Address { get; set; }
        public int Size { get; set; }
        public byte[] NativeCode { get; set; }
        public DetourMode Mode { get; set; }

        internal DetourContext(byte[] nativeCode)
        {
            NativeCode = nativeCode;
            Mode = DetourMode.Trampoline;
        }

        internal DetourContext(IntPtr address, int size)
        {
            Address = address;
            Size = size;
            Mode = DetourMode.DirectCall;
        }
    }
}
