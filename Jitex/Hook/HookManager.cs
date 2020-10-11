using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Jitex.Utils.NativeAPI.Windows.Kernel32;

namespace Jitex.Hook
{
    internal sealed class HookManager
    {
        private readonly IList<VTableHook> _hooks = new List<VTableHook>();

        /// <summary>
        /// Inject a delegate in memory
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

        public bool RemoveHook(Delegate del)
        {
            VTableHook hookFound = _hooks.FirstOrDefault(h => h.Delegate.Method.Equals(del.Method));

            if (hookFound == null)
                return false;

            return RemoveHook(hookFound);
        }

        private bool RemoveHook(VTableHook hook)
        {
            WritePointer(hook.Address, hook.OriginalAddress);
            _hooks.Remove(hook);
            return true;
        }

        private void WritePointer(IntPtr address, IntPtr pointer)
        {
            VirtualProtect(address, new IntPtr(IntPtr.Size), MemoryProtection.ReadWrite, out MemoryProtection oldFlags);
            Marshal.WriteIntPtr(address, pointer);
            VirtualProtect(address, new IntPtr(IntPtr.Size), oldFlags, out _);
        }
    }
}