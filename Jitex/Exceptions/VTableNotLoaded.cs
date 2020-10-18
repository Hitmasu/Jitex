using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Exceptions
{
    internal class VTableNotLoaded : Exception
    {
        public VTableNotLoaded(string vtable) : base($"VTable {vtable} not loaded!")
        {

        }
    }
}
