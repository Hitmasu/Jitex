namespace Jitex.Tests.Context;

public class InstanceConstructor
{
    public int Number { get; set; }
    public Person Person { get; set; }

    public InstanceConstructor()
    {
        Number = -1;
    }

    public InstanceConstructor(int number, Person person)
    {
        Number = number;
        Person = person;
    }
}