using System;
using System.Reflection;

namespace Jitex.Attributes
{
    public enum DetourMode
    {
        ByToken,
        ByName
    }

    public abstract class DetourAttribute : Attribute, IEquatable<MethodBase>
    {
        public DetourMode DetourMode { get; }

        protected DetourAttribute(DetourMode detourMode)
        {
            DetourMode = detourMode;
        }

        public abstract bool Equals(MethodBase other);
    }
}