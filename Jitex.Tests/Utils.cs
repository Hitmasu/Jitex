using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Jitex.Tests
{
    internal static class Utils
    {
        public static MethodInfo GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name, (BindingFlags) (-1)).GetBaseDefinition();
        }
    }
}