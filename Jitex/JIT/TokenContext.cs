using Jitex.JIT.CorInfo;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.JIT
{
    public class TokenContext
    {
        /// <summary>
        /// Instância do CEEInfo
        /// </summary>
        private static CEEInfo _ceeInfo;

        private CORINFO_RESOLVED_TOKEN _resolvedToken;
        internal CORINFO_RESOLVED_TOKEN ResolvedToken => _resolvedToken;

        /// <summary>
        /// Tipo do Token
        /// </summary>
        public TokenKind TokenType => _resolvedToken.tokenType;

        /// <summary>
        /// Modulo do Token
        /// </summary>
        public IntPtr Scope => _resolvedToken.tokenScope;
        
        /// <summary>
        /// Contexto do Token (Tipos genéricos)
        /// </summary>
        public IntPtr Context => _resolvedToken.tokenContext;

        /// <summary>
        /// Metadata Token
        /// </summary>
        public int MetadataToken => _resolvedToken.token;
        
        /// <summary>
        /// Handle do Método
        /// </summary>
        public IntPtr MethodHandle => _resolvedToken.hMethod;

        /// <summary>
        /// Handle do Field
        /// </summary>
        public IntPtr FieldHandle => _resolvedToken.hField;

        /// <summary>
        /// Módulo original do token
        /// </summary>
        public Module Module { get; }

        /// <summary>
        /// Origem da chamada
        /// </summary>
        public MemberInfo Source { get; set; }


        /// <summary>
        /// Se o Token já foi resolvido.
        /// </summary>
        public bool IsResolved { get; internal set; }

        internal TokenContext(ref CORINFO_RESOLVED_TOKEN resolvedToken, MemberInfo source, CEEInfo ceeInfo)
        {
            _ceeInfo ??= ceeInfo;

            _resolvedToken = resolvedToken;

            Module = AppModules.GetModuleByPointer(resolvedToken.tokenScope);

            Source = source;
        }

        public void ResolveFromModule(Module module)
        {
            IsResolved = true;

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
            }
        }

        public void ResolveMethod(MethodBase method)
        {
            IsResolved = true;

            if (method is DynamicMethod)
                throw new NotImplementedException();

            _resolvedToken.tokenScope = _ceeInfo.GetMethodModule(method.MethodHandle.Value);
            _resolvedToken.token = method.MetadataToken;
        }

        public void ResolveConstructor(ConstructorInfo constructor)
        {
            ResolveMethod(constructor);
        }
    }
}