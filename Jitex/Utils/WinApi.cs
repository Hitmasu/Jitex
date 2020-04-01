using System;
using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    public static class WinApi
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
            ExecuteReadWrite = 0x40,
            ReadWrite = 0x04
        }

        [DllImport("kernel32", EntryPoint = "VirtualAlloc")]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32", EntryPoint = "VirtualFree")]
        public static extern int VirtualFree(IntPtr lpAddress, IntPtr dwSize, FreeType freeType);

        [DllImport("kernel32", EntryPoint = "VirtualProtect")]
        public static extern int VirtualProtect(IntPtr lpAddress, IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);
    }
}