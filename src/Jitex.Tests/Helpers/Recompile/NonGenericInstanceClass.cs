using Jitex.Tests.Helpers.Attributes;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Jitex.Tests.Helpers.Recompile
{
    [ClassRecompileTest]
    class NonGenericInstanceClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NonGeneric() { }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public async Task StubAsync(){}

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic<T>() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticNonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticGeneric<T>() { }
    }
}
