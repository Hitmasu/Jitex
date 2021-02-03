using System;
using System.Buffers;

namespace Jitex.Utils
{
    internal class MemoryHelper
    {
        public static unsafe MemoryHandle PinAddress(IntPtr address)
        {
            Memory<IntPtr> memory = new Memory<IntPtr>(new[] { address});
            return memory.Pin();
        }
    }
}
