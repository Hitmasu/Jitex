using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception for when Jitex is not loaded.
    /// </summary>
    public class JitexNotEnabledException : Exception
    {
        /// <summary>
        /// Default exception message.
        /// </summary>
        public JitexNotEnabledException(string message) : base(message)
        {
            
        }
    }
}
