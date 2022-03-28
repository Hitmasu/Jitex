using System;
using System.Runtime.CompilerServices;
using Jitex.Utils;

namespace Jitex.Intercept
{
    internal class VariableInfo
    {
        public Type Type { get; }
        private readonly Pointer _pointer;
        private object? _value;

        public VariableInfo(Pointer pointer, Type type)
        {
            Type = type;
            _pointer = pointer;
        }

        public IntPtr GetAddress() => _pointer.Address;

        public object? GetValue() => MarshalHelper.GetObjectFromAddress(_pointer.Address, Type);

        public unsafe ref T? GetValueRef<T>()
        {
            if (Type.IsByRef)
            {
                void** refPtr = (void**) _pointer.Ptr;

                return ref Unsafe.AsRef<T>(*refPtr)!;
            }

            return ref Unsafe.AsRef<T>(_pointer.Ptr)!;
        }

        public void SetValue<T>(ref T value)
        {
            if (Type.IsByRef)
            {
                unsafe
                {
                    void** refPtr = (void**) _pointer.Ptr;
                    *refPtr = Unsafe.AsPointer(ref value);
                }
            }
            else
            {
                SetValue(value);
            }
        }

        public void SetValue<T>(T value)
        {
            ref T? refValue = ref GetValueRef<T>();

            if (Unsafe.IsNullRef(ref refValue) && Type.IsByRef && !Type.IsValueType)
            {
                unsafe
                {
                    _value = value!; //To prevent GC collect variable
                    void*** refPtr = (void***) _pointer.Ptr;
                    *refPtr = (void**) Unsafe.AsPointer(ref _value);
                    refValue = ref GetValueRef<T>();
                }
            }

            refValue = value;
        }
    }
}