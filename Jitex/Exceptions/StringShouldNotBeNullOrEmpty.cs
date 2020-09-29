using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception for when String is null or empty (string injection)
    /// </summary>
    public class StringShouldNotBeNullOrEmpty : Exception
    {
        public StringShouldNotBeNullOrEmpty() : base ("String should not be null or empty!")
        {
            
        }
    }
}
