using System;
using System.Runtime.InteropServices;

namespace Jitex.Utils.NativeAPI.Windows
{
    internal static class Kernel32
    {
        [Flags]
        public enum AllocationType
        {
            Commit = 0x1000
        }

        [Flags]
        public enum FreeType
        {
            Release = 0x8000,
        }

        [Flags]
        public enum MemoryProtection
        {
            NONE = 0x0,
            EXECUTE = 0x10,
            EXECUTE_READ = 0x20,
            EXECUTE_READ_WRITE = 0x40,
            EXECUTE_WRITE_COPY = 0x80,
            NO_ACCESS = 0x01,
            READ_ONLY = 0x02,
            READ_WRITE = 0x04,
            WRITE_COPY = 0x08,
            GUARD_MODIFIERFLAG = 0x100,
            NO_CACHE_MODIFIERFLAG = 0x200,
            WRITE_COMBINE_MODIFIERFLAG = 0x400
        }

        [DllImport("kernel32", EntryPoint = "VirtualAlloc")]
        private static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32", EntryPoint = "VirtualFree")]
        private static extern int VirtualFree(IntPtr lpAddress, IntPtr dwSize, FreeType freeType);

        [DllImport("kernel32", EntryPoint = "VirtualProtect")]
        private static extern int VirtualProtect(IntPtr lpAddress, IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);

        public static IntPtr VirtualAlloc(int size, AllocationType allocationType, MemoryProtection protection)
        {
            return VirtualAlloc(IntPtr.Zero, size, allocationType, protection);
        }

        public static int VirtualFree(IntPtr address, int size)
        {
            return VirtualFree(address, new IntPtr(size), FreeType.Release);
        }

        public static MemoryProtection VirtualProtect(IntPtr address, int size, MemoryProtection protection)
        {
            VirtualProtect(address, new IntPtr(size), protection, out MemoryProtection oldFlags);
            return oldFlags;
        }
    }
}