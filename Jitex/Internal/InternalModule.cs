using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.JIT.Context;

namespace Jitex.Internal
{
    internal class InternalModule : JitexModule
    {
        private static InternalModule? _instance;

        private InternalModule()
        {

        }

        private readonly ConcurrentDictionary<MethodBase, IList<TokenScope>> _internalTokens = new ConcurrentDictionary<MethodBase, IList<TokenScope>>();

        public static InternalModule GetInstance()
        {
            return _instance ??= new InternalModule();
        }

        protected override void MethodResolver(MethodContext context)
        {
        }

        protected override void TokenResolver(TokenContext context)
        {
            if (context.Source == null)
                return;

            if (_internalTokens.TryGetValue(context.Source, out IList<TokenScope> scopes))
            {
                TokenScope? scope = scopes.FirstOrDefault(w => w.MetadataToken == context.MetadataToken);

                if (scope == null)
                    return;

                //context.ResolveToken(scope.Module,scope.TokenReplace);

                scopes.Remove(scope);

                if (scopes.Count == 0)
                {
                    _internalTokens.TryRemove(context.Source, out _);
                }
            }
        }

        public void AddTokenScope(MethodBase source, TokenScope scope)
        {
            //TODO: Find a better way
            _internalTokens.AddOrUpdate(source, new List<TokenScope> { scope }, (key, tokens) =>
            {
                tokens.Add(scope);
                return tokens;
            });
        }
    }
}
