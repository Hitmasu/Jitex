using System;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Context for token resolution.
    /// </summary>
    public class TokenContext
    {
        private ResolvedToken _resolvedToken;

        /// <summary>
        /// Token type.
        /// </summary>
        public TokenKind TokenType { get; }

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
        internal TokenContext(ref ResolvedToken resolvedToken, MemberInfo source)
        {
            _resolvedToken = resolvedToken;
            Module = _resolvedToken.Module;
            Source = source;

            TokenType = _resolvedToken.Type;
            Context = _resolvedToken.Context;
            MetadataToken = _resolvedToken.Token;

            switch (TokenType)
            {
                case TokenKind.Method:
                    Handle = _resolvedToken.HMethod;
                    break;

                case TokenKind.Field:
                    Handle = _resolvedToken.HField;
                    break;

                case TokenKind.Class:
                    Handle = _resolvedToken.HClass;
                    break;
            }
        }

        /// <summary>
        /// Constructor for string type.
        /// </summary>
        /// <param name="constructString">Original string.</param>
        /// <param name="source">Source method from compile tree ("requester").</param>
        internal TokenContext(ConstructString constructString, MemberInfo source)
        {
            Module = AppModules.GetModuleByAddress(constructString.HandleModule);
            Source = source;

            TokenType = TokenKind.String;
            MetadataToken = constructString.MetadataToken;

            if (Module != null)
                Content = Module.ResolveString(MetadataToken);
        }

        /// <summary>
        /// Resolve token by module.
        /// </summary>
        /// <param name="module">Module containing token.</param>
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
        /// Resolve token by method.
        /// </summary>
        /// <param name="method">Method to replace.</param>
        public void ResolveMethod(MethodBase method)
        {
            IsResolved = true;

            if (method is DynamicMethod)
                throw new NotImplementedException();

            _resolvedToken.Module = method.Module;
            _resolvedToken.Token = method.MetadataToken;
        }

        /// <summary>
        /// Resolve token by constructor.
        /// </summary>
        /// <param name="constructor">Constructor to replace.</param>
        public void ResolveConstructor(ConstructorInfo constructor)
        {
            ResolveMethod(constructor);
        }

        /// <summary>
        /// Resolve string by content string.
        /// </summary>
        /// <param name="content">Content to replace.</param>
        public void ResolveString(string content)
        {
            IsResolved = true;
            Content = content;
        }
    }
}