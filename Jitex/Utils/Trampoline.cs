using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using Jitex.Utils.NativeAPI;
using Jitex.Utils.NativeAPI.POSIX;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;

namespace Jitex.Utils
{
    internal static class Trampoline
    {
        private static readonly byte[] TrampolineInstruction;

        static Trampoline()
        {
            TrampolineInstruction = new byte[]
            {
                    // mov rax, 0000000000000000h ;Pointer to delegate
                    0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // jmp rax
                    0xFF, 0xE0
            };
        }

        /// <summary>
        /// Create a trampoline 64 bits.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IntPtr AllocateTrampoline(IntPtr address)
        {
            IntPtr jmpNative;

            if (OSPlatformHelper.IsPosix)
                jmpNative = Mman.mmap(TrampolineInstruction.Length, MmapProts.PROT_EXEC | MmapProts.PROT_WRITE, MmapFlags.MAP_ANON | MmapFlags.MAP_SHARED);
            else
                jmpNative = Kernel32.VirtualAlloc(TrampolineInstruction.Length, Kernel32.AllocationType.Commit, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);

            Marshal.Copy(TrampolineInstruction, 0, jmpNative, TrampolineInstruction.Length);
            Marshal.WriteIntPtr(jmpNative, 2, address);
            return jmpNative;
        }

        /// <summary>
        /// Free memory trampoline.
        /// </summary>
        /// <param name="address"></param>
        public static void FreeTrampoline(IntPtr address)
        {
            if (OSPlatformHelper.IsPosix)
                Mman.munmap(address, TrampolineInstruction.Length);
            else
                Kernel32.VirtualFree(address, TrampolineInstruction.Length);
        }
    }
}