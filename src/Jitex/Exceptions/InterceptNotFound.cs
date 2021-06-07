using System;
using System.Reflection;

namespace Jitex.Exceptions
{
    public class InterceptNotFound : Exception
    {
        public InterceptNotFound(MethodBase method) : base($"Intercept to method {method.Name} was not found.")
        {
            
        }

        public InterceptNotFound() : base("Intercept method was not found.")
        {
            
        }

        public InterceptNotFound(string message) : base(message)
        {
            
        }
    }
}
