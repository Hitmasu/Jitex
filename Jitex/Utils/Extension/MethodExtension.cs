using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Jitex.Utils.Extension
{
    internal static class MethodExtension
    {
        private static readonly Type TaskNonGeneric;
        private static readonly Type TaskGeneric;
        private static readonly Type ValueTask;
        private static readonly Type ValueTaskGeneric;

        static MethodExtension()
        {
            TaskNonGeneric = typeof(Task);
            TaskGeneric = typeof(Task<>);

            ValueTask = typeof(ValueTask);
            ValueTaskGeneric = typeof(ValueTask<>);
        }

        /// <summary>
        /// Return if method is a awaitable method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsAwaitable(this MethodBase method)
        {
            if (method.IsConstructor)
                return false;

            MethodInfo methodInfo = (MethodInfo)method;
            Type returnType = methodInfo.ReturnType;

            if (returnType.IsGenericType)
                returnType = returnType.GetGenericTypeDefinition();

            return returnType == TaskGeneric || returnType == TaskNonGeneric ||
                   returnType == ValueTask || returnType == ValueTaskGeneric;
        }

        
    }
}
