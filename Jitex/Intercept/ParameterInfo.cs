using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Utils;

namespace Jitex.Intercept
{
    public class Parameters : IEnumerable<Parameter>, IDisposable
    {
        private readonly Parameter[] _parameters;

        public object? this[int index]
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
        public T? GetParameterValue<T>(int index)
        {
            ref object value = ref GetParameterValue(index);

            if (value == null)
                return default;

            return (T?)value;
        }

        /// <summary>
        /// Get ref parameter value from index.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <returns>Value from parameter.</returns>
        public ref object? GetParameterValue(int index)
        {
            Parameter parameter = GetParameter(index);
            return ref parameter.GetValue();
        }

        /// <summary>
        /// Set value to parameter.
        /// </summary>
        /// <param name="index">Index of parameter.</param>
        /// <param name="value">Value to set.</param>
        public void SetParameterValue(int index, object value)
        {
            Parameter parameter = GetParameter(index);
            parameter.SetValue(value);
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
            if (address == IntPtr.Zero) throw new ArgumentNullException(nameof(address));

            Parameter parameter = GetParameter(index);

            if (parameter.ElementType.IsValueType)
            {
                int sizeType = Marshal.SizeOf(parameter.ElementType);

                unsafe
                {
                    Span<byte> source = new Span<byte>(address.ToPointer(), sizeType);
                    Span<byte> dest = new Span<byte>(parameter.AddressValue.ToPointer(), sizeType);

                    source.CopyTo(dest);
                }
            }
            else
            {
                address = Marshal.ReadIntPtr(address);
                Marshal.WriteIntPtr(parameter.AddressValue, address);
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

        public void Dispose()
        {
            foreach (Parameter parameter in _parameters)
                parameter.Dispose();
        }
    }

    /// <summary>
    /// Information about parameter method.
    /// </summary>
    public class Parameter : IDisposable
    {
        private object? _value;
        private IntPtr _address;
        private IntPtr _addressValue;

        /// <summary>
        /// Value of parameter.
        /// </summary>
        public object? Value => _value;

        /// <summary>
        /// Type of parameter.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// If address setted is from return original method.
        /// </summary>
        internal bool IsReturnAddress { get; set; }

        /// <summary>
        /// Address of parameter.
        /// </summary>
        internal IntPtr AddressValue
        {
            get
            {
                if (_addressValue == IntPtr.Zero)
                    return GetAddressValue();

                return _addressValue;
            }
        }

        /// <summary>
        /// "Real type" from parameter (case parameter is ByRef)
        /// </summary>
        internal Type ElementType { get; }

        /// <summary>
        /// Value from parameter (Address or Value)
        /// </summary>
        /// <remarks>
        /// It's necessary to difference how value is passed to a method. Eg.:
        /// ValueType -> Pass directly value to method.
        /// ReferenceType -> Pass a IntPtr (reference address) to method.
        /// </remarks>
        public object? RealValue
        {
            get
            {
                if (Type.IsPrimitive)
                    return _value;

                return AddressValue;
            }
        }

        /// <summary>
        /// Create parameter from address.
        /// </summary>
        /// <param name="address">Address of parameter.</param>
        /// <param name="type">Type of parameter.</param>
        /// <param name="readValue">If value should be read.</param>
        /// <param name="isReturnAddress">If address is a return from original method.</param>
        internal Parameter(IntPtr address, Type type, bool readValue = true, bool isReturnAddress = false)
        {
            IsReturnAddress = isReturnAddress;
            Type = type ?? throw new ArgumentNullException(nameof(type));

            if (type.IsByRef)
                ElementType = type.GetElementType()!;
            else
                ElementType = type;

            //Normally, we dont should store address, because the value address can be updated (moved by GC)
            //and stored address become outdated.
            //But case parameter is ByRef, it's necessary store case we need modify later.
            if(Type.IsByRef || isReturnAddress)
                SetAddress(address);

            if (readValue)
            {
                if (address == IntPtr.Zero)
                    _value = null;
                else if (ElementType.IsValueType)
                    _value = Marshal.PtrToStructure(address, ElementType);
                else
                    _value = TypeHelper.GetObjectFromReference(address);
            }
        }

        /// <summary>
        /// Create parameter from ref value.
        /// </summary>
        /// <param name="value">Value of parameter.</param>
        /// <param name="type">Type of parameter.</param>
        internal Parameter(ref object value, Type type)
        {
            _value = value;
            Type = type;
        }

        /// <summary>
        /// Create parameter from value.
        /// </summary>
        /// <param name="value">Value of parameter.</param>
        /// <param name="type">Type of parameter.</param>
        internal Parameter(object value, Type type) : this(ref value, type) { }
        
        internal void SetValue(object value)
        {
            _value = value;
            SetAddress(IntPtr.Zero);
        }

        internal void SetValue(ref object value)
        {
            _value = value;
            IntPtr address = TypeHelper.GetReferenceFromObject(ref _value);
            SetAddress(address);
        }

        internal void SetAddress(IntPtr address, bool readValue)
        {
            SetAddress(address);

            if (readValue)
                _value = TypeHelper.GetObjectFromReference(address);
        }

        /// <summary>
        /// Read "real address" from parameter.
        /// </summary>
        /// <remarks>
        /// A address from parameter have some difference, Eg.:
        /// ValueType is passed a value address directly
        /// ReferenceType is passed a reference address which pointer to a value address.
        /// </remarks>
        /// <returns>Return RealAddress from parameter.</returns>
        private IntPtr GetAddressValue()
        {
            IntPtr address;

            if (_address == IntPtr.Zero)
            {
                if (_value == null)
                    return IntPtr.Zero;

                address = TypeHelper.GetReferenceFromObject(ref _value);
            }
            else if (IsReturnAddress)
            {
                if (Type.IsValueType)
                    address = _address - IntPtr.Size;
                else
                    address = _address;

                return address;
            }
            else
            {
                address = _address;
            }

            return TypeHelper.GetValueAddress(address, Type);
        }

        /// <summary>
        /// Set address to parameter.
        /// </summary>
        /// <param name="address">Address to set.</param>
        private void SetAddress(IntPtr address)
        {
            _address = address;

            IntPtr addressValue = GetAddressValue();

            _addressValue = addressValue;
        }

        internal ref object? GetValue()
        {
            return ref _value;
        }

        public void Dispose()
        {
            _address = IntPtr.Zero;
            _addressValue = IntPtr.Zero;
        }
    }
}