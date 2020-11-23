using System.Reflection;

namespace Jitex.JIT.Context
{
    internal class SelfToken
    {
        public MethodBase Source { get; set; }
        public int Token { get; set; }
    }
}
