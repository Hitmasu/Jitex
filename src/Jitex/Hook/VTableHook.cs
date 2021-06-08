using System;

namespace Jitex.Hook
{
    internal sealed class VTableHook
    {
        public Delegate Delegate { get; }

        /// <summary>
        ///     Original address
        /// </summary>
        public IntPtr OriginalAddress { get; }

        /// <summary>
        ///     New address.
        /// </summary>
        public IntPtr Address { get; }

        public VTableHook(Delegate @delegate, IntPtr originalAddress, IntPtr address)
        {
            Delegate = @delegate;
            OriginalAddress = originalAddress;
            Address = address;
        }
    }
}