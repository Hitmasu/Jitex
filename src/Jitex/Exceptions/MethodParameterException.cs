using System;
using System.Reflection;

namespace Jitex.Exceptions
{
    public class MethodParameterException : Exception
    {
        public MethodParameterException(MethodBase method) : base($"{method.Name} dont have parameters.")
        {

        }
    }
}
