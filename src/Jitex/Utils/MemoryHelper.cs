using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;
using Jitex.Utils.NativeAPI.POSIX;

namespace Jitex.Utils
{
    internal static class MemoryHelper
    {
        private static readonly object LockSelfMemLinux = new object();

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
            byte[] trampoline = GetTrampoline(address);
            IntPtr jmpNative;

            if (OSHelper.IsPosix)
                jmpNative = Mman.mmap(trampoline.Length, MmapProts.PROT_EXEC | MmapProts.PROT_WRITE, MmapFlags.MAP_ANON | MmapFlags.MAP_SHARED);
            else
                jmpNative = Kernel32.VirtualAlloc(trampoline.Length, Kernel32.AllocationType.Commit, Kernel32.MemoryProtection.EXECUTE_READ_WRITE);

            Marshal.Copy(trampoline, 0, jmpNative, trampoline.Length);
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

        public static void UnprotectWrite<T>(IntPtr address, int offset, T value) where T : unmanaged => UnprotectWrite(address + offset, value);

        /// <summary>
        /// Remove protection from address and write value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">Address to unprotect and write.</param>
        /// <param name="value">Value to write.</param>
        public static void UnprotectWrite<T>(IntPtr address, T value) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();

            if (OSHelper.IsWindows)
            {
                Kernel32.MemoryProtection oldFlags = Kernel32.VirtualProtect(address, size, Kernel32.MemoryProtection.READ_WRITE);
                Write(address, value);
                Kernel32.VirtualProtect(address, size, oldFlags);
            }
            else if (OSHelper.IsLinux)
            {
                byte[] byteValue;

                unsafe
                {
                    void* ptr = Unsafe.AsPointer(ref value);
                    byteValue = new Span<byte>(ptr, size).ToArray();
                }

                lock (LockSelfMemLinux)
                {
                    //Prevent segmentation fault.
                    using FileStream fs = File.Open($"/proc/self/mem", FileMode.Open, FileAccess.ReadWrite);
                    fs.Seek(address.ToInt64(), SeekOrigin.Begin);
                    fs.Write(byteValue, 0, byteValue.Length);
                }
            }
            else
            {
                Mman.mprotect(address, (ulong) size, MmapProts.PROT_WRITE);
                Write(address, value);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Write<T>(IntPtr address, int offset, T value) => Write(address + offset, value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe void Write<T>(IntPtr address, T value) => Unsafe.Write(address.ToPointer(), value);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T Read<T>(IntPtr address, int offset) => Read<T>(address + offset);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe T Read<T>(IntPtr address) => Unsafe.Read<T>(address.ToPointer());

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static T ReadUnaligned<T>(IntPtr address, int offset) => ReadUnaligned<T>(address + offset);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static unsafe T ReadUnaligned<T>(IntPtr address) => Unsafe.ReadUnaligned<T>(address.ToPointer());
    }
}