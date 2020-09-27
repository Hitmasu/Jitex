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

        public FieldInfo ResolveField(int token)
        {
            return _module.ResolveField(token);
        }

        public FieldInfo ResolveField(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return _module.ResolveField(token, genericTypeArguments, genericMethodArguments);
        }

        public MemberInfo ResolveMember(int token)
        {
            return _module.ResolveMember(token);
        }

        public MemberInfo ResolveMember(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return _module.ResolveMember(token, genericTypeArguments, genericMethodArguments);
        }

        public MethodBase ResolveMethod(int token)
        {
            return _module.ResolveMethod(token);
        }

        public MethodBase ResolveMethod(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return _module.ResolveMethod(token, genericTypeArguments, genericMethodArguments);
        }

        public byte[] ResolveSignature(int token)
        {
            return _module.ResolveSignature(token);
        }

        public string ResolveString(int token)
        {
            return _module.ResolveString(token);
        }

        public Type ResolveType(int token)
        {
            return _module.ResolveType(token);
        }

        public Type ResolveType(int token, Type[] genericTypeArguments, Type[] genericMethodArguments)
        {
            return _module.ResolveType(token, genericTypeArguments, genericMethodArguments);
        }
    }
}