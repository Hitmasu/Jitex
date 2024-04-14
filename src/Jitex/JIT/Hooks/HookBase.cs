using System;
using System.Linq;
using System.Runtime.InteropServices;
using Jitex.Framework;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.JIT.Hooks;

internal abstract class HookBase
{
    protected static ThreadTls? Tls;
    protected static readonly RuntimeFramework Framework = RuntimeFramework.Framework;
}

internal abstract class HookBase<THookDelegate> : HookBase, IDisposable
    where THookDelegate : Delegate
{
    private bool _isHookPrepared;
    private IntPtr _indexAddress;
    private IntPtr _originalAddress;
    private IntPtr _hookAddress;

    public bool IsEnabled { get; private set; }


    private static Delegate? Handlers { get; set; }
    protected THookDelegate MethodHook { get; set; }

    protected HookBase(THookDelegate methodHook)
    {
        MethodHook = methodHook;
    }

    public void InjectHook(IntPtr indexAddress)
    {
        PrepareHook();

        _indexAddress = indexAddress;

        if (_originalAddress == default)
            _originalAddress = MemoryHelper.Read<IntPtr>(_indexAddress);

        if (_hookAddress == default)
            _hookAddress = Marshal.GetFunctionPointerForDelegate(MethodHook);

        MemoryHelper.UnprotectWrite(_indexAddress, _hookAddress);
        IsEnabled = true;
    }

    public void RemoveHook()
    {
        if (_indexAddress == default || _originalAddress == default)
            return;

        MemoryHelper.UnprotectWrite(_indexAddress, _originalAddress);
        IsEnabled = false;
    }

    public void ReInjectHook()
    {
        InjectHook(_indexAddress);
    }

    public void AddHandler<THandler>(THandler handler) where THandler : Delegate
    {
        Handlers = (THandler)Delegate.Combine(Handlers, handler);
    }

    public void RemoverHandler<THandler>(THandler handler) where THandler : Delegate
    {
        Handlers = Delegate.Remove(Handlers, handler) as THandler;
    }

    internal bool HasHandler<THandler>(THandler handler) where THandler : Delegate
    {
        return GetInvocationList<THandler>()
            .Any(del => del.Method == handler.Method);
    }

    protected static THandler[] GetInvocationList<THandler>() where THandler : Delegate
    {
        if (Handlers == null)
            return [];

        return (THandler[])Handlers.GetInvocationList();
    }

    public void PrepareHook()
    {
        if (_isHookPrepared)
            return;

        //Prepare default value from parameters.
        var parameters =
            MethodHook.Method.GetParameters()
                .Select(p =>
                {
                    //Just a hack to bypass out parameter on CompileHook
                    if (p.ParameterType.IsByRef)
                        return 0;
                    return Activator.CreateInstance(p.ParameterType);
                })
                .ToArray();

        RuntimeHelperExtension.PrepareDelegate(MethodHook, parameters);

        _isHookPrepared = true;
    }

    public virtual void Dispose()
    {
        RemoveHook();
        Handlers = null;
        GC.SuppressFinalize(this);
    }
}