using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Jitex.Utils.NativeAPI.Windows;

#if !Windows
    using System.IO;
    using Jitex.Utils;
    using Jitex.Utils.NativeAPI.POSIX;
    using Mono.Unix.Native;
#endif

namespace Jitex.Hook
{
    internal sealed class HookManager
    {
        private readonly IList<VTableHook> _hooks = new List<VTableHook>();

        /// <summary>
        /// Inject a delegate in VTable.
        /// </summary>
        /// <param name="pointerAddress">Pointer to address method.</param>
        /// <param name="delToInject">Delegate to be inject.</param>
        public void InjectHook(IntPtr pointerAddress, Delegate delToInject)
        {
            IntPtr originalAddress = Marshal.ReadIntPtr(pointerAddress);
            IntPtr hookAddress = Marshal.GetFunctionPointerForDelegate(delToInject);
            VTableHook hook = new VTableHook(delToInject, originalAddress, pointerAddress);
            WritePointer(pointerAddress, hookAddress);
            _hooks.Add(hook);
        }

        /// <summary>
        /// Remove hook from VTable.
        /// </summary>
        /// <param name="del">Delegate to remove.</param>
        /// <returns></returns>
        public bool RemoveHook(Delegate del)
        {
            VTableHook? hookFound = _hooks.FirstOrDefault(h => h.Delegate.Method.Equals(del.Method));

            return hookFound != null && RemoveHook(hookFound);
        }

        private bool RemoveHook(VTableHook hook)
        {
            WritePointer(hook.Address, hook.OriginalAddress);
            _hooks.Remove(hook);
            return true;
        }

        /// <summary>
        /// Write pointer on address.
        /// </summary>
        /// <param name="address">Address to write pointer.</param>
        /// <param name="pointer">Pointer to write.</param>
        private static void WritePointer(IntPtr address, IntPtr pointer)
        {
#if Windows
            Kernel32.MemoryProtection oldFlags = Kernel32.VirtualProtect(address, IntPtr.Size, Kernel32.MemoryProtection.READ_WRITE);
            Marshal.WriteIntPtr(address, pointer);
            Kernel32.VirtualProtect(address, IntPtr.Size, oldFlags);
            
#elif Linux
                byte[] newAddress = BitConverter.GetBytes(pointer.ToInt64());

                //Prevent segmentation fault.
                using FileStream fs = File.Open($"/proc/{ProcessInfo.PID}/mem", FileMode.Open, FileAccess.ReadWrite);
                fs.Seek(address.ToInt64(), SeekOrigin.Begin);
                fs.Write(newAddress, 0, newAddress.Length);	
#else
                Mman.mprotect(address, (ulong)IntPtr.Size, MmapProts.PROT_WRITE);
                Marshal.WriteIntPtr(address, pointer);
#endif
        }
    }
}