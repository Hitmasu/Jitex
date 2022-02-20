using System;
using System.Diagnostics;

namespace Jitex.Builder.Method
{
    /// <summary>
    /// Information about local variables.
    /// </summary>
    [DebuggerDisplay("{Type.Name}")]
    public class LocalVariableInfo
    {
        /// <summary>
        /// Type from local variable.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// If variable is pinned.
        /// </summary>
        public bool IsPinned { get; set; }

        /// <summary>
        /// Create a local variable from type.
        /// </summary>
        /// <param name="type">Type of local variable.</param>
        public LocalVariableInfo(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Create a local variable from type.
        /// </summary>
        /// <param name="type">Type of local variable.</param>
        /// <param name="isPinned">Variable is pinned.</param>
        public LocalVariableInfo(Type type, bool isPinned)
        {
            Type = type;
            IsPinned = isPinned;
        }
    }
}