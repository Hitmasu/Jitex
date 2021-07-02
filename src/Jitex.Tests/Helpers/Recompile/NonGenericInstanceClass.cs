﻿using Jitex.Tests.Helpers.Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jitex.Tests.Helpers.Recompile
{
    [ClassRecompileTest]
    class NonGenericInstanceClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic<T>() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticNonGeneric() { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticGeneric<T>() { }
    }
}
