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
        private ResolvedToken? _resolvedToken;

        private TokenKind _tokenType;

        /// <summary>
        /// Token type.
        /// </summary>
        public TokenKind TokenType
        {
            get
            {
                if (_resolvedToken != null)
                    return _resolvedToken.Type;

                return _tokenType;
            }
            internal set => _tokenType = value;
        }

        /// <summary>
        /// Address context from token (to generic types).
        /// </summary>
        public IntPtr Context
        {
            get => _resolvedToken.Context;
            set => _resolvedToken.Context = value;
        }

        public IntPtr Scope
        {
            get => _resolvedToken.Scope;
            set => _resolvedToken.Scope = value;
        }

        private int _metadataToken;

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

        private Module _module;

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
                        return _resolvedToken.HMethod;

                    case TokenKind.Field:
                        return _resolvedToken.HField;

                    case TokenKind.Class:
                        return _resolvedToken.HClass;

                    default:
                        throw new NotImplementedException();
                }
            }
            set
            {
                switch (TokenType)
                {
                    case TokenKind.Method:
                        _resolvedToken.HMethod = value;
                        break;

                    case TokenKind.Field:
                        _resolvedToken.HField = value;
                        break;

                    case TokenKind.Class:
                        _resolvedToken.HClass = value;
                        break;

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
                    return _resolvedToken.Module;

                return _module;
            }
            set
            {
                if (_resolvedToken != null)
                    _resolvedToken.Module = value;
                _module = value;
            }
        }

        /// <summary>
        /// Source from compile tree ("requester compile").
        /// </summary>
        public MethodBase? Source { get; internal set; }

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
        internal TokenContext(ref ResolvedToken resolvedToken, MethodBase? source)
        {
            _resolvedToken = resolvedToken;
            Source = source;
        }

        /// <summary>
        /// Constructor for string type.
        /// </summary>
        /// <param name="constructString">Original string.</param>
        /// <param name="source">Source method from compile tree ("requester").</param>
        internal TokenContext(ConstructString constructString, MethodBase? source)
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

        public void ResolveModule(Module module)
        {
            _resolvedToken!.Module = module;
        }

        /// <summary>
        /// Resolve token by method.
        /// </summary>
        /// <param name="method">Method to replace.</param>
        public void ResolveMethod(MethodBase method)
        {
            // if (method is DynamicMethod)
            //     throw new NotImplementedException();

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

        public void ResolveContext()
        {
            _resolvedToken.Context = IntPtr.Zero;
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