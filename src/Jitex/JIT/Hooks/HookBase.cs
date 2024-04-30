using System;
using System.Diagnostics;
using System.Linq;
using Jitex.Framework;
using Jitex.JIT.Hooks.Token;
using Jitex.Utils;

namespace Jitex.JIT.Hooks;

internal abstract class HookBase : IDisposable
{
    private IntPtr _indexAddress;
    private IntPtr _originalAddress;
    protected IntPtr HookAddress { get; set; }

    public bool IsEnabled { get; private set; }
    protected Delegate? Handlers { get; set; }

    public void InjectHook(IntPtr indexAddress)
    {
        _indexAddress = indexAddress;

        if (_originalAddress == default)
            _originalAddress = MemoryHelper.Read<IntPtr>(_indexAddress);

        MemoryHelper.UnprotectWrite(_indexAddress, HookAddress);
        IsEnabled = true;
    }

    public void RemoveHook()
    {
        if (_indexAddress == default || _originalAddress == default)
            return;

        MemoryHelper.UnprotectWrite(_indexAddress, _originalAddress);
        IsEnabled = false;
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
    
    protected Delegate[] GetInvocationList ()
    {
        if (Handlers == null)
            return [];
        
        return Handlers.GetInvocationList();
    }


    protected THandler[] GetInvocationList<THandler>()where THandler : Delegate
    {
        if (Handlers == null)
            return [];

        return Handlers.GetInvocationList().Cast<THandler>().ToArray();
    }

    public abstract void PrepareHook();

    public virtual void Dispose()
    {
        RemoveHook();
        Handlers = null;
        GC.SuppressFinalize(this);
    }
}