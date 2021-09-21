using System;
using System.Reflection;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

namespace Jitex.JIT.Context
{
    /// <summary>
    /// Context for token resolution.
    /// </summary>
    public class TokenContext : ContextBase
    {
        private readonly ResolvedToken? _resolvedToken;
        private TokenKind _tokenType;
        private int _metadataToken;
        private Module? _module;

        /// <summary>
        /// Token type.
        /// </summary>
        public TokenKind TokenType
        {
            get => _resolvedToken?.Type ?? _tokenType;
            internal set => _tokenType = value;
        }

        /// <summary>
        /// Address context from token (to generic types).
        /// </summary>
        public IntPtr Context
        {
            get
            {
                if (TokenType == TokenKind.String)
                    throw new InvalidOperationException("String don't have context.");

                return _resolvedToken!.Context;
            }
            set
            {
                if (TokenType == TokenKind.String)
                    throw new InvalidOperationException("String don't have context.");

                _resolvedToken!.Context = value;
            }
        }

        public IntPtr Scope
        {
            get
            {
                if (TokenType == TokenKind.String)
                    throw new InvalidOperationException("String don't have scope.");

                return _resolvedToken!.Scope;
            }

            set
            {
                if (TokenType == TokenKind.String)
                    throw new InvalidOperationException("String don't have scope.");

                _resolvedToken!.Scope = value;
            }
        }

        /// <summary>
        /// Metadata Token
        /// </summary>
        public int MetadataToken
        {
            get
            {
                if (_resolvedToken != null)
                    return _resolvedToken!.Token;

                return _metadataToken;
            }
            set
            {
                if (_resolvedToken != null)
                    _resolvedToken!.Token = value;

                _metadataToken = value;
            }
        }

        /// <summary>
        /// Address handle from token
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                switch (TokenType)
                {
                    case TokenKind.Method:
                        return _resolvedToken!.HMethod;

                    case TokenKind.Field:
                        return _resolvedToken!.HField;

                    case TokenKind.Class:
                        return _resolvedToken!.HClass;

                    case TokenKind.String:
                        throw new InvalidOperationException("String don't have handle.");

                    default:
                        throw new NotImplementedException();
                }
            }
            set
            {
                switch (TokenType)
                {
                    case TokenKind.Method:
                        _resolvedToken!.HMethod = value;
                        break;

                    case TokenKind.Field:
                        _resolvedToken!.HField = value;
                        break;

                    case TokenKind.Class:
                        _resolvedToken!.HClass = value;
                        break;

                    case TokenKind.String:
                        throw new InvalidOperationException("String don't have handle.");

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>
        /// Source module from token.
        /// </summary>
        public Module? Module
        {
            get
            {
                if (_resolvedToken != null)
                    return _resolvedToken!.Module;

                return _module;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Module can't be null.");

                if (_resolvedToken != null)
                    _resolvedToken!.Module = value;

                _module = value;
            }
        }

        /// <summary>
        /// If context is already resolved.
        /// </summary>
        public bool IsResolved { get; private set; }

        /// <summary>
        /// Content from string (only to string).
        /// </summary>
        public string? Content { get; private set; }

        /// <summary>
        /// Constructor for token type. (non-string)
        /// </summary>
        /// <param name="resolvedToken">Original token.</param>
        /// <param name="source">Source method from compile tree ("requester").</param>
        /// <param name="hasSource">Has source from call.</param>
        internal TokenContext(ref ResolvedToken resolvedToken, MethodBase? source, bool hasSource) : base(source, hasSource)
        {
            _resolvedToken = resolvedToken;
        }

        /// <summary>
        /// Constructor for string type.
        /// </summary>
        /// <param name="constructString">Original string.</param>
        /// <param name="source">Source method who requested token.</param>
        /// /// <param name="hasSource">Has source from call.</param>
        internal TokenContext(ConstructString constructString, MethodBase? source, bool hasSource) : base(source, hasSource)
        {
            _module = AppModules.GetModuleByHandle(constructString.HandleModule);

            TokenType = TokenKind.String;
            MetadataToken = constructString.MetadataToken;

            if (Module != null)
                Content = Module.ResolveString(MetadataToken);
        }

        /// <summary>
        /// Resolve token from module.
        /// </summary>
        /// <param name="module">Module containing token.</param>
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
            }
        }

        /// <summary>
        /// Resolve token by method.
        /// </summary>
        /// <param name="method">Method to replace.</param>
        public void ResolveMethod(MethodBase method)
        {
            _resolvedToken!.Module = method.Module;
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