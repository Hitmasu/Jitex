using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Framework.Offsets;
using Jitex.JIT.CorInfo;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.JIT.Hooks.Token;

/// <summary>
/// Token resolver handler.
/// </summary>
/// <param name="context">Context of token.</param>
public delegate void TokenResolverHandler(TokenContext context);

internal class TokenHook : HookBase
{
    private static CEEInfo.ResolveTokenDelegate DelegateHook;

    [ThreadStatic]
    private static ThreadTls? _tls;

    private static TokenHook? Instance { get; set; }

    public static TokenHook GetInstance()
    {
        Instance ??= new TokenHook();
        return Instance;
    }

    private void Hook(IntPtr thisHandle, IntPtr pResolvedToken)
    {
        _tls ??= new ThreadTls();
        _tls.EnterCount++;

        if (thisHandle == IntPtr.Zero)
        {
            CompileMethod.CompileMethodHook.RegisterSource(IntPtr.Zero, null);
            return;
        }

        var token = 0;

        try
        {
            if (_tls.EnterCount > 1)
            {
                CEEInfo.ResolveToken(thisHandle, pResolvedToken);
                return;
            }

            var resolvers = GetInvocationList<TokenResolverHandler>();

            if (!resolvers.Any())
            {
                CEEInfo.ResolveToken(thisHandle, pResolvedToken);
                return;
            }

            var resolvedToken = new ResolvedToken(pResolvedToken);
            token = resolvedToken.Token; //Just to show on exception.

            MethodBase? source = null;

            if (!OSHelper.IsX86)
            {
                var sourceAddress =
                    Marshal.ReadIntPtr(thisHandle, IntPtr.Size * ResolvedTokenOffset.SourceOffset);
                if (sourceAddress != default)
                    source = MethodHelper.GetMethodFromHandle(sourceAddress);
            }

            var hasSource = source != null;

            var context = new TokenContext(ref resolvedToken, source, hasSource);

            foreach (var resolver in resolvers)
            {
                resolver(context);
            }

            CEEInfo.ResolveToken(thisHandle, pResolvedToken);

            if (resolvedToken.HMethod != IntPtr.Zero)
            {
                CompileMethod.CompileMethodHook.RegisterSource(resolvedToken.HMethod, source);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to resolve token: 0x{token:X}.", ex);
        }
        finally
        {
            _tls.EnterCount--;
        }
    }

    public override void PrepareHook()
    {
        DelegateHook = Hook;
        HookAddress = Marshal.GetFunctionPointerForDelegate(DelegateHook);
        RuntimeHelperExtension.PrepareDelegate(DelegateHook, IntPtr.Zero, IntPtr.Zero);
    }

    
    public void SetNewInstanceTls()
    {
        _tls = new ThreadTls();
    }
}