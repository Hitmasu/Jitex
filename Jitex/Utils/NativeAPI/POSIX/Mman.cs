using System;
using Mono.Unix.Native;

namespace Jitex.Utils.NativeAPI.POSIX
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Interface to mman.h from POSIX.
    /// </summary>
    internal static class Mman
    {
        /// <summary>
        /// Allocate memory block.
        /// </summary>
        /// <param name="length">Length to allocate.</param>
        /// <param name="prot">Permissions.</param>
        /// <param name="flag">Visiblity.</param>
        /// <returns>Address of block allocated.</returns>
        public static IntPtr mmap(ulong length, MmapProts prot, MmapFlags flag)
        {
            return Syscall.mmap(IntPtr.Zero, length, prot, flag, 0, 0);
        }

        /// <summary>
        /// Free memory block.
        /// </summary>
        /// <param name="address">Address of block.</param>
        /// <param name="length">Size of block.</param>
        /// <returns>True to success | False to failed.</returns>
        public static bool munmap(IntPtr address, ulong length)
        {
            return Syscall.munmap(address, length) == 0;
        }
    }
}
