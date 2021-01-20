using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Utils
{
    /// <summary>
    /// Utilities for type.
    /// </summary>
    internal static class TypeUtils
    {
        private static readonly MethodInfo ToObject;
        private static readonly Func<IntPtr, object> GetInstance;

        static TypeUtils()
        {
            ToObject = typeof(TypedReference).GetMethod(nameof(TypedReference.ToObject))!;
            GetInstance = CreateMethodToObject();
        }

        /// <summary>
        /// Create a method to get value from address.
        /// </summary>
        /// <remarks>
        /// Normally, TypedReference only works using __makeref and with an object already "declared".
        /// In some cases, we dont have information about object, just a pointer to value (normally in return values).
        /// This method, pass address of value directly to __makeref, without use ldloca.s or ldarga.s.
        /// </remarks>
        /// <returns></returns>
        private static Func<IntPtr, object> CreateMethodToObject()
        {
            //TODO: Make return ref object.
            DynamicMethod ptrToObjectMethod = new DynamicMethod("PtrToObject", typeof(object), new[] { typeof(IntPtr) });
            ILGenerator? generator = ptrToObjectMethod.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Mkrefany, typeof(string));
            generator.Emit(OpCodes.Call, ToObject);
            generator.Emit(OpCodes.Ret);

            return (Func<IntPtr, object>)ptrToObjectMethod.CreateDelegate(typeof(Func<IntPtr, object>));
        }

        /// <summary>
        /// Get reference address from object.
        /// </summary>
        /// <param name="obj">Object to get address.</param>
        /// <returns>Reference address from object.</returns>
        public static unsafe IntPtr GetAddressFromObject(ref object obj)
        {
            TypedReference typeRef = __makeref(obj);
            return *(IntPtr*)(&typeRef);
        }

        /// <summary>
        /// Get reference address from object.
        /// </summary>
        /// <param name="obj">Object to get address.</param>
        /// <returns>Reference address from object.</returns>
        public static unsafe IntPtr GetAddressFromObject(ref string obj)
        {
            TypedReference typeRef = __makeref(obj);
            return *(IntPtr*)(&typeRef);
        }

        /// <summary>
        /// Get object from a reference address.
        /// </summary>
        /// <param name="address">Reference address.</param>
        /// <returns>Object from reference.</returns>
        public static object GetObjectFromReference(IntPtr address)
        {
            return GetInstance(address);
        }
    }
}