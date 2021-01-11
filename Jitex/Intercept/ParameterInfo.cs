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
            OverrideParameterValue(index, ref value);
        }

        public void OverrideParameterValue(int index, ref object value)
        {
            IntPtr newReference = TypeUtils.GetAddressFromObject(ref value);
            IntPtr newValue = Marshal.ReadIntPtr(newReference);

            OverrideParameterValue(index, newValue);
        }

        public unsafe void OverrideParameterValue(int index, TypedReference reference)
        {
            IntPtr newReference = *(IntPtr*)&reference;
            IntPtr newValue = Marshal.ReadIntPtr(newReference);

            OverrideParameterValue(index, newValue);
        }

        public void OverrideParameterValue(int index, IntPtr address)
        {
            if (address == IntPtr.Zero) throw new ArgumentException($"Invalid address. Parameter: {nameof(address)}");

            Parameter parameter = GetParameter(index);
            Marshal.WriteIntPtr(parameter.Address, address);
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

        public object Value => _value;

        internal IntPtr Address { get; set; }

        internal object RealValue => Address == IntPtr.Zero ? Value : Address;

        internal Parameter(object value)
        {
            _value = value;
        }

        internal Parameter(IntPtr address)
        {
            Address = address;
            _value = TypeUtils.GetObjectFromReference(address);
        }

        internal void SetValue(ref object value)
        {
            _value = value;
            Address = TypeUtils.GetAddressFromObject(ref value);
        }

        internal void SetAddress(IntPtr address, bool readValue)
        {
            Address = address;

            if (readValue)
                _value = TypeUtils.GetObjectFromReference(address);
        }

        internal ref object GetValue()
        {
            return ref _value;
        }
    }
}