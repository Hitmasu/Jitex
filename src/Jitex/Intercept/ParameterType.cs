using System;

namespace Jitex.Intercept
{
    internal class ParameterType
    {
        public Type? OriginalType { get; }
        public Type Type { get; }

        public ParameterType(Type? originalType, Type type)
        {
            OriginalType = originalType;
            Type = type;
        }
    }
}