using System;
using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context
{
    public class Caller
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetIdade()
        {
            Person person = new Person();
            return person.Idade;
        }
    }

    public class Person
    {
        public int Idade { get; set; } = 100;
    }
}
