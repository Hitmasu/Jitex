using System;

namespace Jitex.Builder.Method.Exceptions
{
    public class TypeNotFoundException : Exception
    {
        public TypeNotFoundException(string message) : base(message)
        {
        }
    }
}