using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MarshalHelper
    {
        private static readonly IntPtr ObjectTypeHandle;

        static MarshalHelper()
        {
            ObjectTypeHandle = typeof(object).TypeHandle.Value;
        }

        /// <summary>
        /// Get reference address from a TypedReference.
        /// </summary>
        /// <remarks>
        /// That is usefull in async methods (where we can't declare TypedReference).
        /// </remarks>
        /// <param name="typeRef">Reference to get address.</param>
        /// <returns>Reference address from TypedReference.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IntPtr GetReferenceFromTypedReference(TypedReference typeRef) => *(IntPtr*) &typeRef;

        /// <summary>
        /// Get reference address from object.
        /// </summary>
        /// <param name="obj">Object to get address.</param>
        /// <returns>Reference address from object.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe IntPtr GetReferenceFromObject(ref object obj)
        {
            TypedReference typeRef = __makeref(obj);
            return *(IntPtr*) &typeRef;
        }

        public static object GetObjectFromAddress(IntPtr address, Type type)
        {
            if (type.IsStruct())
            {
                return GetStructFromAddress(address, type);
            }

            return GetObjectFromReference(address);
        }

        /// <summary>
        /// Get object from a reference address.
        /// </summary>
        /// <param name="address">Reference address.</param>
        /// <param name="typeHandle">Type handle.</param>
        /// <returns>Object from reference.</returns>
        /// 
        /// Created by: IllidanS4
        /// https://github.com/IllidanS4/SharpUtils/blob/a3b4da490537e361e6a5debc873c303023d83bf1/Unsafe/Pointer.cs#L58
        public static object GetObjectFromReference(IntPtr address)
        {
            TypedReference tr = default;
            Span<IntPtr> spanTr;

            unsafe
            {
                spanTr = new Span<IntPtr>(&tr, sizeof(TypedReference));
            }

            spanTr[0] = address;
            spanTr[1] = ObjectTypeHandle;
            return __refvalue(tr, object);
        }

        private static object GetStructFromAddress(IntPtr address, Type type)
        {
            object unitializedObject = FormatterServices.GetUninitializedObject(type);
            IntPtr unitializedObjRef = GetReferenceFromObject(ref unitializedObject);

            int size = TypeHelper.SizeOf(type);
            Span<byte> source;
            Span<byte> dest;

            unsafe
            {
                unitializedObjRef = *(IntPtr*) unitializedObjRef;
                unitializedObjRef += IntPtr.Size;
                source = new Span<byte>(address.ToPointer(), size);
                dest = new Span<byte>(unitializedObjRef.ToPointer(), size);
            }

            source.CopyTo(dest);

            return unitializedObject;
        }
    }
}