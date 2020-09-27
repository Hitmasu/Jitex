using System;
using Jitex.JIT.Context;

namespace Jitex.Modules
{
    public abstract class JitexModule : JitexBase, IDisposable
    {
        protected JitexModule()
        {
            if (!IsInstalled)
                Load();
        }

        protected override void Load()
        {
            if (!IsInstalled)
            {
                base.Load();
                AddCompileResolver(CompileResolver);
                AddTokenResolver(TokenResolver);
            }
        }

        protected abstract void CompileResolver(CompileContext context);

        protected abstract void TokenResolver(TokenContext context);

        public void Dispose()
        {
            RemoveCompileResolver(CompileResolver);
            RemoveTokenResolver(TokenResolver);
        }
    }
}