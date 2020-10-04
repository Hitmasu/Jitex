using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception for when String is null or empty (string injection)
    /// </summary>
    public class StringNullOrEmptyException : Exception
    {
        public StringNullOrEmptyException(string message) : base(message)
        {

        }

        public StringNullOrEmptyException() : base("String cannot be null or empty!")
        {

        }
    }
}
