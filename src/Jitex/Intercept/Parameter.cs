using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using Jitex.Utils;

namespace Jitex.Intercept
{
    internal class Parameter
    {
        private readonly Type _type;
        private readonly object? _value;
        private readonly Pointer? _pointer;

        public Parameter(object value, Type type)
        {
            _value = value;
            _type = type;
            _pointer = _value as Pointer;
        }

        public IntPtr? GetAddress() => _pointer?.Address;

        public object? GetValue() => _pointer?.Unbox(_type);

        public ref T? GetValueRef<T>()
        {
            if (_pointer == null)
                return ref Unsafe.NullRef<T>()!;

            return ref _pointer.Unbox<T>()!;
        }
    }
}