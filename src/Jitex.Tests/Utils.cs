using System;
using System.Reflection;

namespace Jitex.Tests
{
    internal static class Utils
    {
        public static MethodInfo GetMethod<T>(string name)
        {
            return typeof(T).GetMethod(name, (BindingFlags) (-1)).GetBaseDefinition();
        }

        public static MethodInfo GetMethod(Type type, string name)
        {
            return type.GetMethod(name, (BindingFlags)(-1)).GetBaseDefinition();
        }

        public static MethodInfo GetMethodInfo(Type baseType, Type genericParameter, string name)
        {
            Type type = baseType.MakeGenericType(genericParameter);
            MethodInfo method = type.GetMethod(name, (BindingFlags)(-1));
            return method;
        }

        public static object CreateInstance(Type baseType, Type genericParameter)
        {
            Type type = baseType.MakeGenericType(genericParameter);
            object instance = Activator.CreateInstance(type);
            return instance;
        }
    }
}