using Jitex;
using Jitex.JIT.Context;
using Jitex.JIT.CorInfo;

namespace InjectCustomString.Library
{
    public static class ExternLibrary
    {
        public static void Initialize()
        {
            JitexManager.AddMethodResolver(context => { });
            JitexManager.AddTokenResolver(TokenResolver);
        }

        private static void TokenResolver(TokenContext context)
        {
            if (context.TokenType == TokenKind.String && context.Content == "Hello World!")
                context.ResolveString("H3110 W0RLD!");
        }
    }
}