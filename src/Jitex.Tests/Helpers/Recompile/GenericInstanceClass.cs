using Jitex.Tests.Helpers.Attributes;
using System.Runtime.CompilerServices;

namespace Jitex.Tests.Helpers.Recompile
{
    [ClassRecompileTest]
    class GenericInstanceClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic<U>() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticNonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticGeneric<U>() { }
    }
}
