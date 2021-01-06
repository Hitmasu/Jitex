using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.JIT
{
    internal class TokenTls : CompileTls
    {
        private static readonly MethodBase CompileMethod;
        private static readonly MethodBase ResolveToken;
        private static readonly MethodBase ConstructStringLiteral;

        static TokenTls()
        {
            CompileMethod = typeof(ManagedJit).GetMethod("CompileMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            ResolveToken = typeof(ManagedJit).GetMethod("ResolveToken", BindingFlags.Instance | BindingFlags.NonPublic);
            ConstructStringLiteral = typeof(ManagedJit).GetMethod("ConstructStringLiteral", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}
