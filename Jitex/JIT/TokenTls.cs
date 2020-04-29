using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.JIT
{
    internal class TokenTls : CompileTls
    {
        private static readonly MethodBase CompileMethod;
        private static readonly MethodBase ResolveToken;

        public MethodBase Root { get; set; }
        public MemberInfo Source { get; set; }

        static TokenTls()
        {
            CompileMethod = typeof(ManagedJit).GetMethod("CompileMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            ResolveToken = typeof(ManagedJit).GetMethod("ResolveToken", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public MethodBase GetSource()
        {
            StackTrace stack = new StackTrace();

            var currentMethod = MethodBase.GetCurrentMethod();
            var frames = stack.GetFrames().Select(m => m.GetMethod()).FirstOrDefault(m => m != CompileMethod
                                                                                    && m != ResolveToken
                                                                                    && m != currentMethod);
            return frames;
        }
    }
}
