using System.Collections;
using System.Collections.Generic;

namespace Jitex.Tests.Context
{
    public record InterceptPerson
    {
        public InterceptPerson(string name, int age)
        {
            Name = name;
            Age = age;
        }

        public string Name { get; set; }
        public int Age { get; set; }


        public int GetAgeAfter10Years()
        {
            return Age + 10;
        }
    }
}