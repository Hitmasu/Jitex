using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Jitex.Utils;

namespace Jitex.Intercept
{
    public class Parameters : IEnumerable<Parameter>
    {
        private readonly Parameter[] _parameters;
        public object this[int index]
        {
            get => GetParameterValue(index);
            set => SetParameterValue(index, ref value);
        }

        internal Parameters(IEnumerable<Parameter> parameters)
        {
            _parameters = parameters.ToArray();
        }

        internal Parameter GetParameter(int index)
        {
            if (_parameters == null)
                throw new NullReferenceException("No parameters loaded.");

            if (index < 0 || index > _parameters.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _parameters[index];
        }

        /// <summary>
        /// Get parameter value from index.
        /// </summary>
        /// <typeparam name="T">Type from parameter.</typeparam>
        /// <param name="index">Index of parameter.</param>
        /// <returns>Value from parameter.</returns>
        public T GetParameterValue<T>(int index)
        {
            ref object value = ref GetParameterValue(index);
            return (T)value;
        }

        /// <summary>
        /// Get ref parameter value from index.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <returns>Value from parameter.</returns>
        public ref object GetParameterValue(int index)
        {
            if (_parameters == null)
                throw new NullReferenceException("No parameters loaded.");

            if (index < 0 || index > _parameters.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ref _parameters[index].GetValue();
        }

        /// <summary>
        /// Set value to parameter.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <param name="value">Value to set.</param>
        public void SetParameterValue(int index, object value)
        {
            SetParameterValue(index, ref value);
        }

        /// <summary>
        /// Set a ref value to parameter.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <param name="value">Ref value to set.</param>
        public void SetParameterValue(int index, ref object value)
        {
            Parameter parameter = GetParameter(index);
            parameter.SetValue(ref value);
        }

        /// <summary>
        /// Set reference to parameter.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <param name="reference">Reference value.</param>
        /// <param name="readValue">If reference value should be read from address.</param>
        public void SetParameterValue(int index, TypedReference reference, bool readValue = true)
        {
            IntPtr address;

            unsafe
            {
                address = *(IntPtr*)&reference;
            }

            SetParameterValue(index, address, readValue);
        }

        /// <summary>
        /// Set reference address to parameter.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <param name="address">Address from reference.</param>
        /// <param name="readValue">If reference value should be read from address.</param>
        public void SetParameterValue(int index, IntPtr address, bool readValue = true)
        {
            if (address == IntPtr.Zero) throw new ArgumentException($"Invalid address. Parameter: {nameof(address)}");

            Parameter parameter = GetParameter(index);
            parameter.SetAddress(address, readValue);
        }

        public void OverrideParameterValue(int index, object value)
        {
            OverrideParameterValue(index, __makeref(value));
        }

        public void OverrideParameterValue(int index, ref object value)
        {
            OverrideParameterValue(index, __makeref(value));
        }

        public unsafe void OverrideParameterValue(int index, TypedReference reference)
        {
            IntPtr referenceAddress = *(IntPtr*)&reference;
            IntPtr address = Marshal.ReadIntPtr(referenceAddress);

            Parameter parameter = GetParameter(index);

            if (parameter.ElementType.IsValueType)
                address += IntPtr.Size;

            OverrideParameterValue(index, address);
        }

        public void OverrideParameterValue(int index, IntPtr address)
        {
            if (address == IntPtr.Zero) throw new ArgumentException($"Invalid address. Parameter: {nameof(address)}");

            Parameter parameter = GetParameter(index);

            if (parameter.ElementType.IsValueType)
            {
                int sizeType = Marshal.SizeOf(parameter.ElementType);

                unsafe
                {
                    Span<byte> source = new Span<byte>(address.ToPointer(), sizeType);
                    Span<byte> dest = new Span<byte>(parameter.Address.ToPointer(), sizeType);

                    source.CopyTo(dest);
                }
            }
            else
            {
                address = Marshal.ReadIntPtr(address);
                Marshal.WriteIntPtr(parameter.Address, address);
            }

        }

        public IEnumerator<Parameter> GetEnumerator()
        {
            return _parameters.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }
    }

    public class Parameter
    {
        private object _value;
        private IntPtr _address;

        public object Value => _value;

        public Type Type { get; }
        internal bool IsOriginalReturn { get; set; }

        internal IntPtr Address
        {
            get
            {
                IntPtr address;

                if (_address == IntPtr.Zero)
                    address = TypeUtils.GetAddressFromObject(ref _value);
                else if (IsOriginalReturn)
                    return _address;
                else
                    address = _address;

                return TypeUtils.GetValueAddress(address, Type);
            }
        }

        internal Type ElementType { get; }

        public object RealValue
        {
            get
            {
                if (Type.IsPrimitive)
                    return _value;

                return Address;
            }
        }

        internal Parameter(IntPtr address, Type type, bool readValue = true)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            _address = address;

            if (readValue)
            {
                if (type.IsByRef)
                    ElementType = type.GetElementType()!;
                else
                    ElementType = type;

                if (ElementType.IsValueType)
                    _value = Marshal.PtrToStructure(address, ElementType);
                else
                    _value = TypeUtils.GetObjectFromReference(address);
            }
        }

        internal Parameter(ref object value, Type type)
        {
            _value = value;
            Type = type;
        }

        internal void SetValue(ref object value)
        {
            _value = value;
            _address = TypeUtils.GetAddressFromObject(ref _value);
        }

        internal void SetAddress(IntPtr address, bool readValue)
        {
            _address = address;

            if (readValue)
                _value = TypeUtils.GetObjectFromReference(address);
        }

        internal ref object GetValue()
        {
            return ref _value;
        }
    }
}