using System;

namespace Jitex.Exceptions
{
    internal class VTableNotLoaded : Exception
    {
        public VTableNotLoaded(string vtable) : base($"VTable {vtable} not loaded!")
        {

        }
    }
}
