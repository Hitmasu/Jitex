using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Jitex.Utils.WinApi;

namespace Jitex.Hook
{
    internal sealed class HookManager
    {
        private readonly IList<VTableHook> _hooks = new List<VTableHook>();

        public void InjectHook(IntPtr addressTarget, Delegate delToInject)
        {
            IntPtr originalAdress = Marshal.ReadIntPtr(addressTarget);
            IntPtr hookAddress = Marshal.GetFunctionPointerForDelegate(delToInject);
            VTableHook hook = new VTableHook(delToInject, originalAdress, hookAddress);
            WritePointer(addressTarget, hookAddress);
            _hooks.Add(hook);
        }

        public bool RemoveHook(Delegate del)
        {
            var hooksFound = _hooks.Count(h => h.Delegate.Method.Equals(del.Method));

            if (hooksFound != 1)
                return false;

            VTableHook hook = _hooks.First();

            return RemoveHook(hook);
        }

        private bool RemoveHook(VTableHook hook)
        {
            WritePointer(hook.Address, hook.OriginalAddress);
            _hooks.Remove(hook);
            return true;
        }

        private void WritePointer(IntPtr address, IntPtr pointer)
        {
            VirtualProtect(address, new IntPtr(IntPtr.Size), MemoryProtection.ReadWrite, out var oldFlags);
            Marshal.WriteIntPtr(address, pointer);
            VirtualProtect(address, new IntPtr(IntPtr.Size), oldFlags, out _);
        }
    }
}