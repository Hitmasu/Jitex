using System;

namespace Jitex.Exceptions
{
    public class InvalidMethodException : Exception
    {
        public InvalidMethodException(string message) : base(message)
        {
        }
    }
}