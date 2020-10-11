using System;
using System.Runtime.InteropServices;
using Mono.Unix.Native;
using static Jitex.Utils.NativeAPI.POSIX.Mman;
using static Jitex.Utils.NativeAPI.Windows.Kernel32;

namespace Jitex.Utils.NativeAPI
{
    public class Native
    {
        private readonly bool _isPosix = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Allocate block of memory.
        /// </summary>
        /// <param name="length">Length to allocate.</param>
        /// <returns>Address of block memory.</returns>
        public IntPtr AllocateMemory(uint length)
        {
            if (_isPosix)
                return mmap(length, MmapProts.PROT_READ | MmapProts.PROT_WRITE, MmapFlags.MAP_ANONYMOUS);
            return VirtualAlloc(IntPtr.Zero, (int)length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
        }

        /// <summary>
        /// Free block of memory.
        /// </summary>
        /// <param name="address">Address of block.</param>
        /// <param name="length">Size of block.</param>
        /// <returns>True to success | False to failed.</returns>
        public bool FreeMemory(IntPtr address, uint length)
        {
            if (_isPosix)
                return munmap(address, length);
            return VirtualFree(address, new IntPtr(length), FreeType.Release) != 0;
        }
    }
}
