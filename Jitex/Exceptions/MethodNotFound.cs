using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Exceptions
{
    public class MethodNotFound : Exception
    {
        public MethodNotFound(IntPtr handle) : base($"Not found a method with handle 0x{handle.ToString("X")}")
        {
            
        }

        public MethodNotFound(long handle) : this(new IntPtr(handle))
        {
        }
    }
}
