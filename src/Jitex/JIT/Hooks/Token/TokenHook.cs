using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Framework.Offsets;
using Jitex.JIT.CorInfo;
using Jitex.JIT.Hooks.CompileMethod;
using Jitex.Utils;
using Jitex.Utils.Extension;

namespace Jitex.JIT.Hooks.Token;

/// <summary>
/// Token resolver handler.
/// </summary>
/// <param name="context">Context of token.</param>
public delegate void TokenResolverHandler(TokenContext context);

internal class TokenHook : HookBase<CEEInfo.ResolveTokenDelegate>
{
    private static TokenHook? Instance { get; set; }

    public static TokenHook GetInstance()
    {
        Instance ??= new TokenHook();
        return Instance;
    }

    public TokenHook() : base(Hook)
    {
    }

    public static void Hook(IntPtr thisHandle, IntPtr pResolvedToken)
    {
        Tls ??= new ThreadTls();
        Tls.EnterCount++;

        if (thisHandle == IntPtr.Zero)
            return;

        int token = 0;

        try
        {
            if (Tls.EnterCount > 1)
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

            ResolvedToken resolvedToken = new ResolvedToken(pResolvedToken);
            token = resolvedToken.Token; //Just to show on exception.

            MethodBase? source = null;

            if (!OSHelper.IsX86)
            {
                IntPtr sourceAddress =
                    Marshal.ReadIntPtr(thisHandle, IntPtr.Size * ResolvedTokenOffset.SourceOffset);
                if (sourceAddress != default)
                    source = MethodHelper.GetMethodFromHandle(sourceAddress);
            }

            bool hasSource = source != null;

            TokenContext context = new TokenContext(ref resolvedToken, source, hasSource);

            foreach (TokenResolverHandler resolver in resolvers)
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
            Tls.EnterCount--;
        }
    }
}