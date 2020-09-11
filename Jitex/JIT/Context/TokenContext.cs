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
        private CORINFO_CONSTRUCT_STRING _constructString;

        internal CORINFO_RESOLVED_TOKEN ResolvedToken => _resolvedToken;
        internal CORINFO_CONSTRUCT_STRING ConstructString => _constructString;

        /// <summary>
        /// Tipo do Token
        /// </summary>
        public TokenKind TokenType { get; }

        /// <summary>
        /// Modulo do Token
        /// </summary>
        public IntPtr Scope { get; }
        
        /// <summary>
        /// Contexto do Token (Tipos genéricos)
        /// </summary>
        public IntPtr Context { get; }

        /// <summary>
        /// Metadata Token
        /// </summary>
        public int MetadataToken { get; }
        
        /// <summary>
        /// Handle do Método
        /// </summary>
        public IntPtr MethodHandle { get; }

        /// <summary>
        /// Handle do Field
        /// </summary>
        public IntPtr FieldHandle { get; }

        public IntPtr ClassHandle { get; }

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
        public bool IsResolved { get; private set; }

        internal bool IsStringResolved { get; private set; }

        public string Content { get; private set; }

        internal TokenContext(ref CORINFO_RESOLVED_TOKEN resolvedToken, MemberInfo source, CEEInfo ceeInfo)
        {
            _ceeInfo ??= ceeInfo;

            _resolvedToken = resolvedToken;

            Module = AppModules.GetModuleByPointer(resolvedToken.tokenScope);
            Source = source;

            TokenType = resolvedToken.tokenType;
            Scope = resolvedToken.tokenScope;
            Context = resolvedToken.tokenContext;
            MetadataToken = resolvedToken.token;
            MethodHandle = resolvedToken.hMethod;
            FieldHandle = resolvedToken.hField;
            ClassHandle = resolvedToken.hClass;
        }

        internal TokenContext(ref CORINFO_CONSTRUCT_STRING constructString, MemberInfo source, CEEInfo ceeInfo)
        {
            _ceeInfo ??= ceeInfo;
            _constructString = constructString;

            Module = AppModules.GetModuleByPointer(constructString.HandleModule);
            Source = source;

            TokenType = TokenKind.String;
            Scope = constructString.HandleModule;
            MetadataToken = constructString.MetadataToken;
            Content = Module.ResolveString(MetadataToken);
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

                case TokenKind.String:
                    ResolveString(MetadataToken,module);
                    break;
            }
        }

        public void ResolveMethod(MethodBase method)
        {
            IsResolved = true;

            if (method is DynamicMethod)
                throw new NotImplementedException();

            _resolvedToken.tokenScope = AppModules.GetPointerFromModule(method.Module);
            _resolvedToken.token = method.MetadataToken;
        }

        public void ResolveString(int metadataToken, Module module)
        {
            IsResolved = true;
            _constructString.MetadataToken = metadataToken;
            _constructString.HandleModule = AppModules.GetPointerFromModule(module);
        }

        public void ResolveString(string content)
        {
            IsStringResolved = true;
            Content = content;
        }

        public void ResolveConstructor(ConstructorInfo constructor)
        {
            ResolveMethod(constructor);
        }
    }
}