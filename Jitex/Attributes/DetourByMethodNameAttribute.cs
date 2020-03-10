using System;
using System.Reflection;

namespace Jitex.Attributes
{
    public class DetourByMethodNameAttribute : DetourAttribute
    {
        public string MethodName { get; }

        public DetourByMethodNameAttribute(string methodName) : base(DetourMode.ByName)
        {
            MethodName = methodName;
        }

        public override bool Equals(MethodBase other)
        {
            return MethodName == other?.Name;
        }
    }
}
