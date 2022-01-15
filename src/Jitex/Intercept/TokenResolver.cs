using System;
using System.Collections.Generic;
using System.Reflection;
using Jitex.Builder.IL.Resolver;

namespace Jitex.Intercept
{
    internal class TokenResolver : ITokenResolver
    {
        private readonly IDictionary<int, MemberInfo> _tokens = new Dictionary<int, MemberInfo>();

        public void AddToken(int token, MemberInfo member)
        {
            if(!_tokens.ContainsKey(token))
                _tokens.Add(token,member);
        }
        
        public FieldInfo? ResolveField(int token, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public FieldInfo? ResolveField(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public MemberInfo? ResolveMember(int token, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public MemberInfo? ResolveMember(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public MethodBase? ResolveMethod(int token, out bool isResolved)
        {
            isResolved = _tokens.TryGetValue(token, out MemberInfo member);
            return member as MethodBase;
        }

        public MethodBase? ResolveMethod(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public Type? ResolveType(int token, out bool isResolved)
        {
            isResolved = _tokens.TryGetValue(token, out MemberInfo member);
            return member as Type;
        }

        public Type? ResolveType(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public byte[]? ResolveSignature(int token, out bool isResolved)
        {
            isResolved = false;
            return null;
        }

        public string? ResolveString(int token, out bool isResolved)
        {
            isResolved = false;
            return null;
        }
    }
}