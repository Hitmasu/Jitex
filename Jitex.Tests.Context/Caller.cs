using System;
using System.Runtime.CompilerServices;

namespace Jitex.Tests.Context
{
    public class Caller
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public int GetIdade()
        {
            Person PERSON = new Person();
            return PERSON.Idade;
        }
    }

    public class Person
    {
        public int Idade { get; set; } = 100;
    }
}
