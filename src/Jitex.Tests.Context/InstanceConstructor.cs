using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context;

public class InstanceConstructor
{
    public int Number { get; set; }
    public Person Person { get; set; }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public InstanceConstructor()
    {
        Number = -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public InstanceConstructor(int number, Person person)
    {
        Number = number;
        Person = person;
    }
}