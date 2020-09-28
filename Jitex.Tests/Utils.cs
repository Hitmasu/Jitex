using System.Reflection;
using Jitex.JIT;

namespace Jitex.Tests
{
    internal static class Utils
    {
        public static MethodInfo GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
        }
    }
}
