using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Jitex.JIT.Context;
using Jitex.JIT.CorInfo;
using Jitex.Utils;

namespace Jitex.Intercept
{
    internal class InternalModule : JitexModule
    {
        public static InternalModule Instance { get; } = new InternalModule();

        /// <summary>
        /// Tokens to be resolved
        /// </summary>
        /// <remarks>
        /// Key: (IntPtr module, int metadataToken)
        /// Value: Member resolution.
        /// </remarks>
        private readonly ConcurrentDictionary<Tuple<IntPtr, int>, MemberInfo> _methodResolutions = new();

        protected override void MethodResolver(MethodContext context)
        {
        }

        protected override void TokenResolver(TokenContext context)
        {
            if (context.Module == null)
                return;

            if (!_methodResolutions.TryGetValue(new(context.Scope, context.MetadataToken), out MemberInfo resolution))
                return;

            context.ResolverMember(resolution);
        }

        /// <summary>
        /// Add a token to be resolved.
        /// </summary>
        /// <param name="module">Module from token.</param>
        /// <param name="metadataToken">Token to be resolved.</param>
        /// <param name="memberResolution">Member identifier from token.</param>
        public void AddMemberToResolution(Module module, int metadataToken, MemberInfo memberResolution)
        {
            IntPtr handle = AppModules.GetHandleFromModule(module);
            _methodResolutions.TryAdd(new(handle, metadataToken), memberResolution);
        }
    }
}