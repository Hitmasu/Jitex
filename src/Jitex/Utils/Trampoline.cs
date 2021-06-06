using System;
using System.Runtime.InteropServices;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;
using Jitex.Utils.NativeAPI.POSIX;

namespace Jitex.Utils
{
    internal static class Trampoline
    {
        private static readonly byte[] TrampolineInstruction =
        {
            // mov rax, 0000000000000000h ;Pointer to delegate
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // jmp rax
            0xFF, 0xE0
        };

        public static int Size => TrampolineInstruction.Length;

        public static byte[] GetTrampoline(IntPtr methodAddress)
        {
            byte[] trampoline = TrampolineInstruction;
            byte[] address = BitConverter.GetBytes(methodAddress.ToInt64());
            address.CopyTo(trampoline, 2);
            return trampoline;
        }

        public static IntPtr AllocateTrampoline(IntPtr address)
        {
            IntPtr jmpNative;

            if (OSHelper.IsPosix)
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
            if (OSHelper.IsPosix)
                Mman.munmap(address, TrampolineInstruction.Length);
            else
                Kernel32.VirtualFree(address, TrampolineInstruction.Length);
        }
    }
}