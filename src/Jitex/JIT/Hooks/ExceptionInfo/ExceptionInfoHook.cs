using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Jitex.JIT.CorInfo;

namespace Jitex.JIT.Hooks.ExceptionInfo;

internal class ExceptionInfoHook : HookBase
{
    public static ExceptionInfoHook? Instance;
    public static IntPtr Handle { get; set; }

    public ExceptionInfoHook()
    {
    }

    public static ExceptionInfoHook GetInstance()
    {
        Instance ??= new ExceptionInfoHook();
        return Instance;
    }

    private static void Hook(IntPtr thisHandle, IntPtr ftn, uint ehNumber, out IntPtr clause)
    {
        CEEInfo.GetEHInfo(thisHandle, ftn, ehNumber, out clause);

        if (ftn == Handle)
        {
            var ehInfo = Marshal.PtrToStructure<CorInfoEhClause>(clause);
            Debugger.Break();
        }
    }

    public override void PrepareHook()
    {
        throw new NotImplementedException();
    }
}