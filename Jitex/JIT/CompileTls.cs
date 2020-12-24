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
        public virtual MethodBase? GetSource()
        {
            StackTrace stack = new StackTrace();

            MethodBase[] trace = stack.GetFrames().Select(m => m.GetMethod()).Where(w => w.Module != GetType().Module).ToArray();

            return trace.Length >= 2 ? trace.ElementAt(1) : trace.LastOrDefault();
        }
    }
}