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

        /// <summary>
        /// Obtém o método que "requisitou" a compilação.
        /// </summary>
        /// <returns></returns>
        public MethodBase GetSource()
        {
            StackTrace stack = new StackTrace();
            
            MethodBase currentMethod = MethodBase.GetCurrentMethod();
            MethodBase frames = stack.GetFrames().Select(m => m.GetMethod()).FirstOrDefault(m => m != CompileMethod
                                                                                                  && m != ResolveToken
                                                                                                  && m != currentMethod);
            return frames;
        }
    }
}
