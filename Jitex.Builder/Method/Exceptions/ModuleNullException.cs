using System;

namespace Jitex.Builder.Method.Exceptions
{
    public class ModuleNullException : Exception
    {
        public ModuleNullException(string message) : base(message)
        {
        }
    }
}