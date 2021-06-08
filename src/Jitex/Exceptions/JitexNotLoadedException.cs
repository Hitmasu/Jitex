using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception for when Jitex is not loaded.
    /// </summary>
    public class JitexNotLoadedException : Exception
    {
        /// <summary>
        /// Default exception message.
        /// </summary>
        public JitexNotLoadedException() : base("Jitex not loaded!")
        {
            
        }
    }
}
