using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception for when String is null or empty (string injection)
    /// </summary>
    public class StringNullOrEmptyException : Exception
    {
        /// <summary>
        /// Custom exception message.
        /// </summary>
        /// <param name="message"></param>
        public StringNullOrEmptyException(string message) : base(message)
        {

        }

        /// <summary>
        /// Default exception message.
        /// </summary>
        public StringNullOrEmptyException() : base("String cannot be null or empty!")
        {

        }
    }
}
