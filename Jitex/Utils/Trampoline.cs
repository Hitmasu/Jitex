using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Jitex.Utils.Extension;
using Jitex.Utils.NativeAPI.Windows;

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
            IntPtr jmpNative = Kernel32.VirtualAlloc(IntPtr.Zero, TrampolineInstruction.Length, Kernel32.AllocationType.Commit, Kernel32.MemoryProtection.ExecuteReadWrite);
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
            Kernel32.VirtualFree(address, new IntPtr(TrampolineInstruction.Length), Kernel32.FreeType.Release);
        }
    }
}