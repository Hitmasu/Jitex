using System;

namespace Jitex.Builder.Method.Exceptions
{
    
    /// <summary>
    /// Exception when type is not referenced on assembly.
    /// </summary>
    public class TypeNotFoundException : Exception
    {
        /// <summary>
        /// Exception message to raise.
        /// </summary>
        /// <param name="message">Message exception.</param>
        public TypeNotFoundException(string message) : base(message)
        {
        }
    }
}