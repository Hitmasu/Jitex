using System;
using System.Runtime.InteropServices;

namespace Jitex.Utils.Extension
{
    internal static class ByteArrayToPointer
    {
        /// <summary>
        /// Return pointer from byte array.
        /// </summary>
        /// <param name="arr">Byte array.</param>
        /// <returns>Pointer to array.</returns>
        public static IntPtr ToPointer(this byte[] arr)
        {
            IntPtr address = Marshal.AllocHGlobal(arr.Length);
            Marshal.Copy(arr, 0, address, arr.Length);
            return address;
        }
    }
}
