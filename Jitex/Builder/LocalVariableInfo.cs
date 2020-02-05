using System;
using System.Reflection;

namespace Jitex.Builder
{
    public class LocalVariableInfo
    {
        private static readonly MethodInfo GetCorElementType;
        private static readonly FieldInfo GetRuntimeType;

        static LocalVariableInfo()
        {
            GetCorElementType = typeof(RuntimeTypeHandle).GetMethod("GetCorElementType", BindingFlags.Static | BindingFlags.NonPublic);
            GetRuntimeType = typeof(RuntimeTypeHandle).GetField("m_type", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public Type Type { get; }

        public CorElementType ElementType
        {
            get
            {
                object runtime = GetRuntimeType.GetValue(Type.TypeHandle);
                object corElementType = GetCorElementType.Invoke(null, new[] {runtime});
                return (CorElementType)(byte)corElementType;
            }
        }

        public LocalVariableInfo(Type type)
        {
            Type = type;
        }
    }
}