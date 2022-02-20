using Jitex.Tests.Helpers.Attributes;
using System.Runtime.CompilerServices;

namespace Jitex.Tests.Helpers.Recompile
{
    [ClassRecompileTest]
    static class NonGenericStaticClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Generic<T>() { }
    }
}
