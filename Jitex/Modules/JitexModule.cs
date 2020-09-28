using System;
using Jitex.JIT.Context;

namespace Jitex.Modules
{
    public abstract class JitexModule : IDisposable
    {
        public bool IsInstalled => Jitex.IsInstalled && Jitex.HasCompileResolver(CompileResolver) && Jitex.HasTokenResolver(TokenResolver);

        protected JitexModule(bool load = true)
        {
            if (load)
                Load();
        }

        protected void Load()
        {
            Jitex.AddCompileResolver(CompileResolver);
            Jitex.AddTokenResolver(TokenResolver);
        }

        protected abstract void CompileResolver(CompileContext context);

        protected abstract void TokenResolver(TokenContext context);

        public void Dispose()
        {
            Jitex.RemoveCompileResolver(CompileResolver);
            Jitex.RemoveTokenResolver(TokenResolver);
        }
    }
}