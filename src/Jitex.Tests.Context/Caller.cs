using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context
{
    public class Caller
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetWrong()
        {
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetAge()
        {
            Person person = new Person();
            return person.Age;
        }
    }

    public class Person
    {
        public int Age { get; set; } = 100;
    }
}
