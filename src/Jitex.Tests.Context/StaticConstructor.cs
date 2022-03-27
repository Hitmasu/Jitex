namespace Jitex.Tests.Context;

public class StaticConstructor
{
    public static int Number { get; set; } 

    static StaticConstructor()
    {
        Number = 20;
    }
}