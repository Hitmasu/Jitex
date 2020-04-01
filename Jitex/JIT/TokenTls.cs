using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.JIT
{
    internal class TokenTls : CompileTls
    {
        private static readonly MethodBase _compileMethod;
        private static readonly MethodBase _resolveToken;

        public MethodBase Root { get; set; }
        public MemberInfo Source { get; set; }

        static TokenTls()
        {
            _compileMethod = typeof(ManagedJit).GetMethod("CompileMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            _resolveToken = typeof(ManagedJit).GetMethod("ResolveToken", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public MethodBase GetSource()
        {
            StackTrace stack = new StackTrace();

            var currentMethod = MethodBase.GetCurrentMethod();
            var frames = stack.GetFrames().Select(m => m.GetMethod()).FirstOrDefault(m => m != _compileMethod
                                                                                    && m != _resolveToken
                                                                                    && m != currentMethod);
            return frames;
        }
    }
}
