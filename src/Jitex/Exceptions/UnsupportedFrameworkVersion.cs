using System;

namespace Jitex.Exceptions
{
    class UnsupportedFrameworkVersion : Exception
    {
        public UnsupportedFrameworkVersion(string message) : base(message)
        {
            
        }
    }
}
