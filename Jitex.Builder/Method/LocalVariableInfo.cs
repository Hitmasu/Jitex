using System;
using System.Diagnostics;
using System.Reflection;

namespace Jitex.Builder.Method
{
    [DebuggerDisplay("{Type.Name}")]
    public class LocalVariableInfo
    {
        private static readonly MethodInfo GetCorElementType;
        private static readonly FieldInfo GetRuntimeType;

        public Type Type { get; }

        public CorElementType ElementType
        {
            get
            {
                return DetectCorElementType(Type);
            }
        }

        static LocalVariableInfo()
        {
            GetCorElementType = typeof(RuntimeTypeHandle).GetMethod("GetCorElementType", BindingFlags.Static | BindingFlags.NonPublic);
            GetRuntimeType = typeof(RuntimeTypeHandle).GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
        }
        
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

        public LocalVariableInfo(Type type)
        {
            Type = type;
        }
    }
}