using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Exceptions
{
    class UnsupportedFrameworkVersion : Exception
    {
        public UnsupportedFrameworkVersion(string message) : base(message)
        {
            
        }
    }
}
