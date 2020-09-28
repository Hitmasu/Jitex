using System;
using Jitex.Exceptions;
using Jitex.JIT.Context;

namespace Jitex.Modules
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
        public bool IsInstalled => Jitex.IsInstalled && Jitex.HasCompileResolver(CompileResolver) && Jitex.HasTokenResolver(TokenResolver);

        /// <summary>
        /// Instance a new module.
        /// </summary>
        /// <param name="load">If module should be loaded on instance.</param>
        protected JitexModule(bool load = true)
        {
            if (load)
                Load();
        }

        /// <summary>
        /// Load module on Jitex (if not already loaded).
        /// </summary>
        protected void Load()
        {
            if(!Jitex.IsInstalled)
                throw new JitexNotLoadedException();

            if (!IsInstalled)
            {
                Jitex.AddCompileResolver(CompileResolver);
                Jitex.AddTokenResolver(TokenResolver);
            }
        }
        
        /// <summary>
        /// Resolver to methods.
        /// </summary>
        /// <remarks>
        /// Capture all methods before compile.
        /// </remarks>
        /// <param name="context">Context of Method will be compiled.</param>
        protected abstract void CompileResolver(CompileContext context);

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
        public void Dispose()
        {
            Jitex.RemoveCompileResolver(CompileResolver);
            Jitex.RemoveTokenResolver(TokenResolver);
        }
    }
}