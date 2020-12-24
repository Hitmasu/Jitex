using System;
using Mono.Unix.Native;

namespace Jitex.Utils.NativeAPI.POSIX
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// Interface to mman.h from POSIX.
    /// </summary>
    internal static class Mman
    {
        /// <summary>
        /// Allocate memory block.
        /// </summary>
        /// <param name="length">Length to allocate.</param>
        /// <returns>Address of block allocated.</returns>
        public static IntPtr mmap(int length, MmapProts prots, MmapFlags flags)
        {
            return Syscall.mmap(IntPtr.Zero, (ulong) length, prots, flags, 0, 0);
        }

        /// <summary>
        /// Free memory block.
        /// </summary>
        /// <param name="address">Address of block.</param>
        /// <param name="length">Size of block</param>
        /// <returns>True to success | False to failed.</returns>
        public static void munmap(IntPtr address, int length)
        {
            Syscall.munmap(address, (ulong) length);
        }

        /// <summary>
        /// Set protection on memory block.
        /// </summary>
        /// <param name="address">Address of block.</param>
        /// <param name="length">Length of block.</param>
        /// <param name="flags">Protection to set.</param>
        /// <returns></returns>
        public static bool mprotect(IntPtr address, ulong length, MmapProts flags)
        {
            return Syscall.mprotect(address, length, flags) == 0;
        }
    }
    // ReSharper restore InconsistentNaming
}
