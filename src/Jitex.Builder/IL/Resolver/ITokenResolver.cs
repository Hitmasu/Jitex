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
    internal interface ITokenResolver
    {
        FieldInfo ResolveField(int token);
        FieldInfo ResolveField(int token, Type[] genericTypeArguments, Type[] genericMethodArguments);
        MemberInfo ResolveMember(int token);
        MemberInfo ResolveMember(int token, Type[] genericTypeArguments, Type[] genericMethodArguments);
        MethodBase ResolveMethod(int token);
        MethodBase ResolveMethod(int token, Type[] genericTypeArguments, Type[] genericMethodArguments);
        byte[] ResolveSignature(int token);
        string ResolveString(int token);
        Type ResolveType(int token);
        Type ResolveType(int token, Type[] genericTypeArguments, Type[] genericMethodArguments);
    }
}