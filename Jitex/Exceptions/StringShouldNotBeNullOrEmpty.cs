using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Exceptions
{
    public class StringShouldNotBeNullOrEmpty : Exception
    {
        public StringShouldNotBeNullOrEmpty() : base ("String should not be null or empty!")
        {
            
        }
    }
}
