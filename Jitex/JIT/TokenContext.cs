using Jitex.JIT.CorInfo;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.JIT
{
    public class TokenContext
    {
        private CORINFO_RESOLVED_TOKEN _resolvedToken;
        private static CEEInfo _ceeInfo;
        internal CORINFO_RESOLVED_TOKEN ResolvedToken => _resolvedToken;

        public TokenKind TokenType => _resolvedToken.tokenType;
        public IntPtr Scope => _resolvedToken.tokenScope;
        public IntPtr Context => _resolvedToken.tokenContext;
        public int MetadataToken => (int) _resolvedToken.token;
        public IntPtr MethodHandle => _resolvedToken.hMethod;
        public IntPtr FieldHandle => _resolvedToken.hField;
        public Module Module { get; }

        public MemberInfo Source { get; set; }

        internal TokenContext(ref CORINFO_RESOLVED_TOKEN resolvedToken, MemberInfo source, CEEInfo ceeInfo)
        {
            if (_ceeInfo == null)
                _ceeInfo = ceeInfo;

            _resolvedToken = resolvedToken;

            Module = AppModules.GetModuleByPointer(resolvedToken.tokenScope);

            Source = source;
        }

        public void ResolveFromModule(Module module)
        {
            switch (TokenType)
            {
                case TokenKind.Newobj:
                    MethodBase newobj = module.ResolveMethod(MetadataToken);
                    ResolveMethod(newobj);
                    break;

                case TokenKind.Method:
                    MethodBase method = module.ResolveMethod(MetadataToken);
                    ResolveMethod(method);
                    break;

                case TokenKind.Field:
                    int a = 10;
                    break;
            }
        }

        public void ResolveMethod(MethodBase method)
        {
            if (method is DynamicMethod)
                throw new NotImplementedException();

            _resolvedToken.tokenScope = _ceeInfo.GetMethodModule(method.MethodHandle.Value);
            _resolvedToken.token = method.MetadataToken;
        }
    }
}