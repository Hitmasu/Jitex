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
        /// <summary>
        /// Method tokens to be resolved
        /// </summary>
        /// <remarks>
        /// Key: (IntPtr methodHandle, int metadataToken)
        /// Value: Method resolution.
        /// </remarks>
        private readonly ConcurrentDictionary<Tuple<IntPtr, int>, MemberInfo> _methodResolutions = new();

        protected override void MethodResolver(MethodContext context)
        {
        }

        protected override void TokenResolver(TokenContext context)
        {
            if (context.Source != null)
            {
                MethodBase source = context.Source;

                if (source == null)
                    return;

                if (source.Name == "Sum" && context.TokenType == TokenKind.Field)
                {
                    Debugger.Break();
                }

                IntPtr methodHandle = MethodHelper.GetMethodHandle(source).Value;

                if (!_methodResolutions.TryGetValue(new(methodHandle, context.MetadataToken), out MemberInfo resolution))
                    return;

                context.ResolverMember(resolution);
            }
        }

        public void AddMethodTokenResolution(MethodBase source, int metadataToken, MemberInfo method)
        {
            _methodResolutions.TryAdd(new(source.MethodHandle.Value, metadataToken), method);
        }
    }
}