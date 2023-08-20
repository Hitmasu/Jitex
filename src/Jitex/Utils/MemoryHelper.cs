using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Jitex.Utils.NativeAPI.Windows;
using Mono.Unix.Native;

namespace Jitex.Utils
{
    internal static class MemoryHelper
    {
        private static readonly object LockSelfMemLinux = new object();

        public static int Size => GetTrampoline().Code.Length;

        public static byte[] GetTrampoline(IntPtr methodAddress)
        {
            var (trampoline, index) = GetTrampoline();
            var address = BitConverter.GetBytes(methodAddress.ToInt64());
            address.CopyTo(trampoline, index);
            return trampoline;
        }

        private static (byte[] Code, int StartIndex) GetTrampoline()
        {
            byte[] trampoline;
            int startIndex;

            if (OSHelper.IsArm64)
            {
                trampoline = new byte[]
                {
                    //ldr x16, .8
                    0x50, 0x00, 0x00, 0x58,

                    //br x16
                    0x00, 0x02, 0x1F, 0xD6,

                    //x64 address
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };

                startIndex = 8;
            }
            else
            {
                trampoline = new byte[]
                {
                    // mov rax, 0000000000000000h ;Pointer to delegate
                    0x48, 0xB8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    // jmp rax
                    0xFF, 0xE0
                };

                startIndex = 2;
            }

            return (trampoline, startIndex);
        }

        public static IntPtr AllocateTrampoline(IntPtr address)
        {
            var trampoline = GetTrampoline(address);

            IntPtr jmpNative;

            if (OSHelper.IsPosix)
            {
                //For Apple Silicon, we cannot set a memory region a WˆX.
                //As we need coverage Linux and OSX, we set as RˆW and RˆX after.
                //See: https://developer.apple.com/documentation/apple-silicon/porting-just-in-time-compilers-to-apple-silicon
                jmpNative = Syscall.mmap(IntPtr.Zero, (ulong)trampoline.Length,
                    MmapProts.PROT_READ | MmapProts.PROT_WRITE, MmapFlags.MAP_PRIVATE | MmapFlags.MAP_ANON, -1, 0);
            }
            else
            {
                jmpNative = Kernel32.VirtualAlloc(trampoline.Length, Kernel32.AllocationType.Commit,
                    Kernel32.MemoryProtection.EXECUTE_READ_WRITE);
            }

            Marshal.Copy(trampoline, 0, jmpNative, trampoline.Length);

            if (OSHelper.IsPosix)
                Syscall.mprotect(jmpNative, (ulong)trampoline.Length, MmapProts.PROT_READ | MmapProts.PROT_EXEC);

            return jmpNative;
        }

        /// <summary>
        /// Free memory trampoline.
        /// </summary>
        /// <param name="address"></param>
        public static void FreeTrampoline(IntPtr address)
        {
            if (OSHelper.IsPosix)
                Syscall.munmap(address, (ulong)Size);
            else
                Kernel32.VirtualFree(address, Size);
        }

        public static void UnprotectWrite<T>(IntPtr address, int offset, T value) where T : unmanaged =>
            UnprotectWrite(address + offset, value);

        /// <summary>
        /// Remove protection from address and write value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">Address to unprotect and write.</param>
        /// <param name="value">Value to write.</param>
        public static void UnprotectWrite<T>(IntPtr address, T value) where T : unmanaged
        {
            var size = Unsafe.SizeOf<T>();

            if (OSHelper.IsWindows)
            {
                var oldFlags = Kernel32.VirtualProtect(address, size, Kernel32.MemoryProtection.READ_WRITE);
                Write(address, value);
                Kernel32.VirtualProtect(address, size, oldFlags);
            }
            else if (OSHelper.IsLinux)
            {
                byte[] byteValue;

                unsafe
                {
                    var ptr = Unsafe.AsPointer(ref value);
                    byteValue = new Span<byte>(ptr, size).ToArray();
                }

                lock (LockSelfMemLinux)
                {
                    //Prevent segmentation fault.
                    using FileStream fs = File.OpenWrite("/proc/self/mem");
                    fs.Seek(address.ToInt64(), SeekOrigin.Begin);
                    fs.Write(byteValue, 0, byteValue.Length);
                }
            }
            else
            {
                //For apple silicon
                if (OSHelper.IsHardenedRuntime)
                {
                    var (alignedAddr, alignedSize) = GetAlignedAddress(address, size);
                    Syscall.mprotect(alignedAddr, alignedSize, MmapProts.PROT_WRITE);
                    Write(address, value);
                    Syscall.mprotect(alignedAddr, alignedSize, MmapProts.PROT_READ);
                }
                else
                {
                    Syscall.mprotect(address, (ulong)size, MmapProts.PROT_WRITE);
                    Write(address, value);
                }
            }
        }

        public static IntPtr GetAlignedAddress(IntPtr address)
        {
            var pageSize = Syscall.sysconf(SysconfName._SC_PAGESIZE);
            var mask = ~(pageSize - 1);
            return (IntPtr)(address.ToInt64() & mask);
        }

        public static (IntPtr address, ulong size) GetAlignedAddress(IntPtr address, int size)
        {
            var alignedAddr = GetAlignedAddress(address);
            var alignedSize = (ulong)(address.ToInt64() - alignedAddr.ToInt64()) + (ulong)size;

            return (alignedAddr, alignedSize);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(IntPtr address, int offset, T value) => Write(address + offset, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(IntPtr address, T value) => Unsafe.Write(address.ToPointer(), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(IntPtr address, int offset) => Read<T>(address + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Read<T>(IntPtr address) => Unsafe.Read<T>(address.ToPointer());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadUnaligned<T>(IntPtr address, int offset) => ReadUnaligned<T>(address + offset);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T ReadUnaligned<T>(IntPtr address) => Unsafe.ReadUnaligned<T>(address.ToPointer());
    }
}