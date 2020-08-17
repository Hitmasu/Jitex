using Jitex.JIT;
using Jitex.JIT.CorInfo;

namespace InjectCustomString.Library
{
    public static class ExternLibrary
    {
        public static void Initialize()
        {
            ManagedJit jitex = ManagedJit.GetInstance();

            jitex.AddCompileResolver(context => { });
            jitex.AddTokenResolver(TokenResolve);
        }

        private static void TokenResolve(TokenContext context)
        {
            if (context.TokenType == TokenKind.String && context.Content == "Hello World!")
                context.ResolveString("H3110 W0RLD!");
        }
    }
}
