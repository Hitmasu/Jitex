using Jitex.Tests.Helpers.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jitex.Tests.Helpers.Recompile
{
    [ClassRecompileTest]
    static class GenericStaticClass<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void NonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Generic<U>() { }
    }
}
