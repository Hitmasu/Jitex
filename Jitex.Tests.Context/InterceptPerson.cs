using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context
{
    public record InterceptPerson
    {
        public InterceptPerson(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public InterceptPerson(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
        public int Age { get; set; }

        [MethodImpl(MethodImplOptions.NoInlining)]

        public int GetAgeAfter10Years()
        {
            return Age + 10;
        }
    }
}