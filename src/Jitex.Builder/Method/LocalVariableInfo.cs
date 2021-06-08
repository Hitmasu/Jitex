using System;
using System.Diagnostics;
using System.Reflection;

namespace Jitex.Builder.Method
{
    /// <summary>
    /// Information about local variables.
    /// </summary>
    [DebuggerDisplay("{Type.Name}")]
    public class LocalVariableInfo
    {
        private static readonly MethodInfo GetCorElementType;
        private static readonly FieldInfo GetRuntimeType;

        /// <summary>
        /// Type from local variable.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Type identifier from variable.
        /// </summary>
        public CorElementType ElementType => DetectCorElementType(Type);

        /// <summary>
        /// Create a local variable from type.
        /// </summary>
        /// <param name="type">Type of local variable.</param>
        public LocalVariableInfo(Type type)
        {
            Type = type;
        }

        static LocalVariableInfo()
        {
            GetCorElementType = typeof(RuntimeTypeHandle).GetMethod("GetCorElementType", BindingFlags.Static | BindingFlags.NonPublic);
            GetRuntimeType = typeof(RuntimeTypeHandle).GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
        /// <summary>
        /// Detect type identifier from a type.
        /// </summary>
        /// <param name="type">Type to detect.</param>
        /// <returns>Identifier from type.</returns>
        public static CorElementType DetectCorElementType(Type type)
        {
            //GetCorElementType will return ELEMENT_TYPE_CLASS to string.
            if (type == typeof(string))
            {
                return CorElementType.ELEMENT_TYPE_STRING;
            }
                
            object runtime = GetRuntimeType.GetValue(type.TypeHandle);
            object corElementType = GetCorElementType.Invoke(null, new[] {runtime});
            return (CorElementType) (byte) corElementType;
        }
    }
}