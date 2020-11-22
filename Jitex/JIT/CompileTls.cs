using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.JIT
{
    internal class CompileTls
    {
        public int EnterCount;

        /// <summary>
        /// Get source from call
        /// </summary>
        /// <returns></returns>
        public virtual MethodBase GetSource()
        {
            StackTrace stack = new StackTrace();
            
            MethodBase source = stack.GetFrames().Select(m => m.GetMethod()).Last();
            return source;
        }
    }
}