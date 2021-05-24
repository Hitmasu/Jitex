using System;

namespace Jitex.Intercept
{
    internal class ParameterType
    {
        public Type? RealType { get; }
        public Type Type { get; }

        public ParameterType(Type? realType, Type type)
        {
            RealType = realType;
            Type = type;
        }
    }
}