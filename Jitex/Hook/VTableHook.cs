using System;

namespace Jitex.Hook
{
    internal sealed class VTableHook
    {
        public Delegate Delegate { get; set; }

        /// <summary>
        /// Original address of delegate
        /// </summary>
        public IntPtr OriginalAddress { get; set; }

        /// <summary>
        /// New address.
        /// </summary>
        public IntPtr Address { get; set; }

        public VTableHook(Delegate @delegate, IntPtr originalAddress, IntPtr address)
        {
            Delegate = @delegate;
            OriginalAddress = originalAddress;
            Address = address;
        }
    }
}
