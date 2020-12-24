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

        /// <summary>
        /// Get source from call
        /// </summary>
        /// <returns></returns>
        public override MethodBase GetSource()
        {
            StackTrace stack = new StackTrace();
            
            MethodBase currentMethod = MethodBase.GetCurrentMethod();
            MethodBase frames = stack.GetFrames().Select(m => m.GetMethod()).FirstOrDefault(m => m != CompileMethod
                                                                                                  && m != ResolveToken
                                                                                                  && m != ConstructStringLiteral
                                                                                                  && m != currentMethod
                                                                                                  );
            return frames;
        }
    }
}
