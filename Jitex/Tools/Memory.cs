using System;
using System.Runtime.InteropServices;
using static Jitex.Tools.WinApi;

namespace Jitex.Tools
{
    internal static class Memory
    {
        private static readonly byte[] TrampolineInstruction =
        {
            // mov rax, 0000000000000000h ;Pointer address to _overrideCompileMethodPtr
            0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            // jmp rax
            0xFF, 0xE0
        };
        
        /// <summary>
        /// Create trampoline a 64 bits. 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static IntPtr AllocateTrampoline(IntPtr address)
        {
            IntPtr jmpNative = VirtualAlloc(IntPtr.Zero, TrampolineInstruction.Length, AllocationType.Commit, MemoryProtection.ExecuteReadWrite);
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
            VirtualFree(address, new IntPtr(TrampolineInstruction.Length), FreeType.Release);
        }
    }
}