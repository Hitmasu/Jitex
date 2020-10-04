using System;

namespace Jitex.Builder.Exceptions
{
    public class ModuleNullException : Exception
    {
        public ModuleNullException(string message) : base(message)
        {
        }

        public ModuleNullException() : base ("Module cannot be null!")
        {
            
        }
    }
}