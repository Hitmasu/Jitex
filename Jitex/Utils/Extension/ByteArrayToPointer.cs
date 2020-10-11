using System;
using System.Runtime.InteropServices;

namespace Jitex.Utils.Extension
{
    internal static class ByteArrayToPointer
    {
        public static IntPtr ToPointer(this byte[] arr)
        {
            IntPtr address = Marshal.AllocHGlobal(arr.Length);
            Marshal.Copy(arr, 0, address, arr.Length);
            return address;
        }
    }
}
