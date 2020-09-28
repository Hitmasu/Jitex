using System;
using System.Collections.Generic;
using System.Text;

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
