using System;

namespace Jitex.Builder.Exceptions
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string message) : base(message)
        {
        }
    }
}