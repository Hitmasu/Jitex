using System;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    public class TokenContext
    {

        /// <summary>
        /// Instance from CEEInfo.
        /// </summary>
        private static CEEInfo _ceeInfo;

        private CORINFO_RESOLVED_TOKEN _resolvedToken;
        private CORINFO_CONSTRUCT_STRING _constructString;

        /// <summary>
        /// Token to be resolved. 
        /// </summary>
        internal CORINFO_RESOLVED_TOKEN ResolvedToken => _resolvedToken;

        /// <summary>
        /// Token type.
        /// </summary>
        public TokenKind TokenType { get; }


        /// <summary>
        /// Address module from token.
        /// </summary>
        public IntPtr Scope { get; }

        /// <summary>
        /// Address context from token (to generic types).
        /// </summary>
        public IntPtr Context { get; }

        /// <summary>
        /// Metadata Token
        /// </summary>
        public int MetadataToken { get; }

        /// <summary>
        /// Address handle from token
        /// </summary>
        public IntPtr Handle { get; set; }

        /// <summary>
        /// Source module from token.
        /// </summary>
        public Module Module { get; }

        /// <summary>
        /// Source from compile tree ("requester compile").
        /// </summary>
        public MemberInfo Source { get; set; }

        /// <summary>
        /// If context is already resolved.
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Content from string (only to string).
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// Constructor for token type. (non-string)
        /// </summary>
        /// <param name="resolvedToken">Original token.</param>
        /// <param name="source">Source method from compile tree ("requester").</param>
        /// <param name="ceeInfo">CEEInfo instance.</param>
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

            switch (TokenType)
            {
                case TokenKind.Method:
                    Handle = resolvedToken.hMethod;
                    break;

                case TokenKind.Field:
                    Handle = resolvedToken.hField;
                    break;

                case TokenKind.Class:
                    Handle = resolvedToken.hClass;
                    break;
            }
        }

        /// <summary>
        /// Constructor for string type.
        /// </summary>
        /// <param name="constructString">Original string.</param>
        /// <param name="source">Source method from compile tree ("requester").</param>
        /// <param name="ceeInfo">CEEInfo instance.</param>
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

        /// <summary>
        /// Resolve token from module.
        /// </summary>
        /// <param name="module">Module to be resolved.</param>
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

        /// <summary>
        /// Resolve token from method.
        /// </summary>
        /// <param name="method">Method to replace.</param>
        public void ResolveMethod(MethodBase method)
        {
            IsResolved = true;

            if (method is DynamicMethod)
                throw new NotImplementedException();

            _resolvedToken.tokenScope = AppModules.GetPointerFromModule(method.Module);
            _resolvedToken.token = method.MetadataToken;
        }

        /// <summary>
        /// Resolve token from constructor.
        /// </summary>
        /// <param name="constructor">Constructor to replace.</param>
        public void ResolveConstructor(ConstructorInfo constructor)
        {
            ResolveMethod(constructor);
        }

        /// <summary>
        /// Resolve string from a new string
        /// </summary>
        /// <param name="content">Content to replace.</param>
        public void ResolveString(string content)
        {
            IsResolved = true;
            Content = content;
        }
    }
}