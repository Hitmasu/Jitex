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
            EXECUTE_READ_WRITE = 0x40,
            READ_WRITE = 0x04,
        }

        public static IntPtr VirtualAlloc(int size, AllocationType allocationType, MemoryProtection protection)
        {
            return Imports.VirtualAlloc(IntPtr.Zero, size, allocationType, protection);
        }

        public static int VirtualFree(IntPtr address, int size)
        {
            return Imports.VirtualFree(address, new IntPtr(size), FreeType.Release);
        }

        public static MemoryProtection VirtualProtect(IntPtr address, int size, MemoryProtection protection)
        {
            Imports.VirtualProtect(address, new IntPtr(size), protection, out MemoryProtection oldFlags);
            return oldFlags;
        }

        private static class Imports
        {
            [DllImport("kernel32", EntryPoint = "VirtualAlloc")]
            internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

            [DllImport("kernel32", EntryPoint = "VirtualFree")]
            internal static extern int VirtualFree(IntPtr lpAddress, IntPtr dwSize, FreeType freeType);

            [DllImport("kernel32", EntryPoint = "VirtualProtect")]
            internal static extern int VirtualProtect(IntPtr lpAddress, IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);
        }
    }
}