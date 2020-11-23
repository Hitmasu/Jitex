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

        private readonly ConcurrentDictionary<MethodBase, IList<int>> _internalTokens = new ConcurrentDictionary<MethodBase, IList<int>>();

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

            if (_internalTokens.TryGetValue(context.Source, out IList<int> tokens))
            {
                if (!tokens.Contains(context.MetadataToken))
                    return;

                tokens.Remove(context.MetadataToken);

                if (!tokens.Any())
                    _internalTokens.TryRemove(context.Source, out _);
            }
        }

        public void AddTokenContext(int token, MethodBase source)
        {
            //TODO: Find a better way
            _internalTokens.AddOrUpdate(source, new List<int> { token }, (key, tokens) =>
            {
                tokens.Add(token);
                return tokens;
            });
        }
    }
}
