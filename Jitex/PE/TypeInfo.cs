using System;
using System.Diagnostics;

namespace Jitex.PE
{
    [DebuggerDisplay("{Type.Name}")]
    internal class TypeInfo
    {
        public TypeInfo(Type type, int rowNumber, TypeIdentifier typeIdentifier)
        {
            RowNumber = rowNumber;
            TypeIdentifier = typeIdentifier;
            Type = type;
        }

        public Type Type { get; }
        public int RowNumber { get; }

        public TypeIdentifier TypeIdentifier { get; set; }
    }
}
