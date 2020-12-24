using System;
using Jitex.JIT.Context;

namespace Jitex
{
    /// <summary>
    /// Base class to create a module to Jitex.
    /// </summary>
    public abstract class JitexModule : IDisposable
    {
        /// <summary>
        /// Return if module is loaded in Jitex.
        /// </summary>
        /// <returns>Return <b>true</b> if module is installed and false if not.</returns>
        public bool IsLoaded => JitexManager.HasMethodResolver(MethodResolver) && JitexManager.HasTokenResolver(TokenResolver);

        /// <summary>
        /// Instance a new module.
        /// </summary>
        protected JitexModule()
        {
        }

        /// <summary>
        /// Load resolver from module.
        /// </summary>
        internal void LoadResolvers()
        {
            JitexManager.AddMethodResolver(MethodResolver);
            JitexManager.AddTokenResolver(TokenResolver);
        }

        /// <summary>
        /// Resolver to methods.
        /// </summary>
        /// <remarks>
        /// Capture all methods before compile.
        /// </remarks>
        /// <param name="context">Context of Method will be compiled.</param>
        protected abstract void MethodResolver(MethodContext context);

        /// <summary>
        /// Resolver to tokens.
        /// </summary>
        /// <remarks>
        /// Capture all tokens before resolution.
        /// </remarks>
        /// <param name="context">Context of Token will be compiled.</param>
        protected abstract void TokenResolver(TokenContext context);

        /// <summary>
        /// Unload module from Jitex.
        /// </summary>
        public virtual void Dispose()
        {
            JitexManager.RemoveMethodResolver(MethodResolver);
            JitexManager.RemoveTokenResolver(TokenResolver);
            JitexManager.RemoveModule(GetType());
        }
    }
}