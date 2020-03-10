using System;
using System.Reflection;

namespace Jitex.IL.Resolver
{
    internal sealed class ModuleTokenResolver : ITokenResolver
    {
        private readonly Module _module;

        public ModuleTokenResolver(Module module)
        {
            _module = module;
        }

        public MemberInfo ResolveMember(int token)
        {
            return _module.ResolveMember(token);
        }

        public Type ResolveType(int token)
        {
            return _module.ResolveType(token);
        }

        public FieldInfo ResolveField(int token)
        {
            return _module.ResolveField(token);
        }

        public MethodBase ResolveMethod(int token)
        {
            return _module.ResolveMethod(token);
        }

        public byte[] ResolveSignature(int token)
        {
            return _module.ResolveSignature(token);
        }

        public string ResolveString(int token)
        {
            return _module.ResolveString(token);
        }
    }
}