using System;
using System.Reflection;

namespace Jitex.Builder.IL.Resolver
{
    internal sealed class ModuleTokenResolver : ITokenResolver
    {
        private readonly Module _module;

        public ModuleTokenResolver(Module module)
        {
            _module = module;
        }

        public FieldInfo ResolveField(int token, out bool isResolved)
        {
            FieldInfo fieldInfo = _module.ResolveField(token);
            isResolved = true;
            return fieldInfo;
        }

        public FieldInfo ResolveField(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            FieldInfo fieldInfo = _module.ResolveField(token, genericTypeArguments, genericMethodArguments);
            isResolved = true;
            return fieldInfo;
        }

        public MemberInfo ResolveMember(int token, out bool isResolved)
        {
            MemberInfo memberInfo = _module.ResolveMember(token);
            isResolved = true;
            return memberInfo;
        }

        public MemberInfo ResolveMember(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            MemberInfo memberInfo = _module.ResolveMember(token, genericTypeArguments, genericMethodArguments);
            isResolved = true;
            return memberInfo;
        }

        public MethodBase ResolveMethod(int token, out bool isResolved)
        {
            MethodBase methodBase = _module.ResolveMethod(token);
            isResolved = true;
            return methodBase;
        }

        public MethodBase ResolveMethod(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            MethodBase methodBase = _module.ResolveMethod(token, genericTypeArguments, genericMethodArguments);
            isResolved = true;
            return methodBase;
        }

        public byte[] ResolveSignature(int token, out bool isResolved)
        {
            byte[] signature = _module.ResolveSignature(token);
            isResolved = true;
            return signature;
        }

        public string ResolveString(int token, out bool isResolved)
        {
            string @string = _module.ResolveString(token);
            isResolved = true;
            return @string;
        }

        public Type ResolveType(int token, out bool isResolved)
        {
            Type type = _module.ResolveType(token);
            isResolved = true;
            return type;
        }

        public Type ResolveType(int token, Type[]? genericTypeArguments, Type[]? genericMethodArguments, out bool isResolved)
        {
            Type type = _module.ResolveType(token, genericTypeArguments, genericMethodArguments);
            isResolved = true;
            return type;
        }
    }
}