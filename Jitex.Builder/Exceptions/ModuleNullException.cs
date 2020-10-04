using System;

namespace Jitex.Builder.Exceptions
{
    /// <summary>
    /// Exception when module is null.
    /// </summary>
    internal class ModuleNullException : Exception
    {
        /// <summary>
        /// Default exception.
        /// </summary>
        public ModuleNullException() : base("Module cannot be null!")
        {

        }

        /// <summary>
        /// Custom exception message.
        /// </summary>
        /// <param name="message">Message to raise.</param>
        public ModuleNullException(string message) : base(message)
        {
        }


    }
}