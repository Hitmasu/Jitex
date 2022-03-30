using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context;

public class StaticConstructor
{
    public static int Number { get; set; } 

    [MethodImpl(MethodImplOptions.NoInlining)]
    static StaticConstructor()
    {
        Number = 20;
    }
}