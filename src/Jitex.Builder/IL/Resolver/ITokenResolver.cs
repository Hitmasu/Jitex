using System;
using System.Reflection;

namespace Jitex.Builder.IL.Resolver
{
    /// <summary>
    ///     Token resolver.
    /// </summary>
    /// <remarks>
    ///     Created by jnm2
    ///     https://stackoverflow.com/a/35711376
    /// </remarks>
    public interface ITokenResolver
    {
        FieldInfo? ResolveField(int token, out bool isResolved);
        FieldInfo? ResolveField(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved);
        MemberInfo? ResolveMember(int token, out bool isResolved);
        MemberInfo? ResolveMember(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved);
        MethodBase? ResolveMethod(int token, out bool isResolved);
        MethodBase? ResolveMethod(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved);
        Type? ResolveType(int token, out bool isResolved);
        Type? ResolveType(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved);
        byte[]? ResolveSignature(int token, out bool isResolved);
        string? ResolveString(int token, out bool isResolved);
    }
}