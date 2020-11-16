using System;
using System.Runtime.InteropServices;
using Jitex.Utils.NativeAPI.Windows;

#if !Windows
    using Mono.Unix.Native;
    using Jitex.Utils.NativeAPI.POSIX;
#endif

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

#if Windows
            IntPtr jmpNative = Kernel32.VirtualAlloc(TrampolineInstruction.Length, Kernel32.AllocationType.Commit, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);
#else
            IntPtr jmpNative = Mman.mmap(TrampolineInstruction.Length, MmapProts.PROT_EXEC | MmapProts.PROT_WRITE, MmapFlags.MAP_ANON | MmapFlags.MAP_SHARED);
#endif

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
#if Windows
            Kernel32.VirtualFree(address, TrampolineInstruction.Length);
#else
            Mman.munmap(address, TrampolineInstruction.Length);
#endif

        }
    }
}