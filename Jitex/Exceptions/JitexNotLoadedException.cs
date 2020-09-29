using System;

namespace Jitex.Exceptions
{
    /// <summary>
    /// Exception to be raise when Jitex is not loaded.
    /// </summary>
    public class JitexNotLoadedException : Exception
    {
        public JitexNotLoadedException() : base("Jitex not loaded!")
        {
            
        }
    }
}
