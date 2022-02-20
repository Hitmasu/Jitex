using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Builder.IL.Resolver
{
    internal class TokenResolver
    {
        private readonly ITokenResolver _resolver;
        public ITokenResolver? CustomResolver { get; set; }

        public TokenResolver(MethodBase method)
        {
            if (method is DynamicMethod dynamicMethod)
                _resolver = new DynamicMethodTokenResolver(dynamicMethod);
            else
                _resolver = new ModuleTokenResolver(method.Module);
        }

        public TokenResolver(Module module)
        {
            _resolver = new ModuleTokenResolver(module);
        }

        public TokenResolver(MethodBase method, ITokenResolver resolver) : this(method)
        {
            CustomResolver = resolver;
        }

        public FieldInfo ResolveField(int token)
        {
            if (CustomResolver != null)
            {
                FieldInfo? fieldInfo = CustomResolver.ResolveField(token, out bool isResolved);

                if (isResolved)
                    return fieldInfo!;
            }

            return _resolver.ResolveField(token, out _)!;
        }

        public FieldInfo ResolveField(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            if (CustomResolver != null)
            {
                FieldInfo? fieldInfo = CustomResolver.ResolveField(token, genericTypeArguments, genericMethodArguments, out bool isResolved);

                if (isResolved)
                    return fieldInfo!;
            }
            
            if (genericMethodArguments == null && genericTypeArguments == null)
                return _resolver.ResolveField(token, out _)!;

            return _resolver.ResolveField(token, genericTypeArguments, genericMethodArguments, out _)!;
        }

        public MemberInfo ResolveMember(int token)
        {
            if (CustomResolver != null)
            {
                MemberInfo? memberInfo = CustomResolver.ResolveMember(token, out bool isResolved);

                if (isResolved)
                    return memberInfo!;
            }

            return _resolver.ResolveMember(token, out _)!;
        }

        public MemberInfo ResolveMember(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            if (CustomResolver != null)
            {
                MemberInfo? memberInfo = CustomResolver.ResolveMember(token, genericTypeArguments, genericMethodArguments, out bool isResolved);

                if (isResolved)
                    return memberInfo!;
            }
            
            if (genericMethodArguments == null && genericTypeArguments == null)
                return _resolver.ResolveMember(token, out _)!;

            return _resolver.ResolveMember(token, genericTypeArguments, genericMethodArguments, out _)!;
        }

        public MethodBase ResolveMethod(int token)
        {
            if (CustomResolver != null)
            {
                MethodBase? methodBase = CustomResolver.ResolveMethod(token, out bool isResolved);

                if (isResolved)
                    return methodBase!;
            }

            return _resolver.ResolveMethod(token, out _)!;
        }

        public MethodBase ResolveMethod(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            if (CustomResolver != null)
            {
                MethodBase? methodBase = CustomResolver.ResolveMethod(token, genericTypeArguments, genericMethodArguments, out bool isResolved);

                if (isResolved)
                    return methodBase!;
            }

            if (genericMethodArguments == null && genericTypeArguments == null)
                return _resolver.ResolveMethod(token, out _)!;

            return _resolver.ResolveMethod(token, genericTypeArguments, genericMethodArguments, out _)!;
        }

        public byte[] ResolveSignature(int token)
        {
            if (CustomResolver != null)
            {
                byte[]? signature = CustomResolver.ResolveSignature(token, out bool isResolved);

                if (isResolved)
                    return signature!;
            }

            return _resolver.ResolveSignature(token, out _)!;
        }

        public string ResolveString(int token)
        {
            if (CustomResolver != null)
            {
                string? @string = CustomResolver.ResolveString(token, out bool isResolved);

                if (isResolved)
                    return @string!;
            }

            return _resolver.ResolveString(token, out _)!;
        }

        public Type ResolveType(int token)
        {
            if (CustomResolver != null)
            {
                Type? type = CustomResolver.ResolveType(token, out bool isResolved);

                if (isResolved)
                    return type!;
            }

            return _resolver.ResolveType(token, out _)!;
        }

        public Type ResolveType(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
        {
            if (CustomResolver != null)
            {
                Type? type = CustomResolver.ResolveType(token, genericTypeArguments, genericMethodArguments, out bool isResolved);

                if (isResolved)
                    return type!;
            }

            if (genericMethodArguments == null && genericMethodArguments == null)
                return _resolver.ResolveType(token, out _)!;

            return _resolver.ResolveType(token, genericTypeArguments, genericMethodArguments, out _)!;
        }
    }
}