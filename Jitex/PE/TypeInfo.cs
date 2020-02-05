using System;
using System.Diagnostics;
using System.Reflection;

namespace Jitex.PE
{
    /// <summary>
    /// Information Type from Stream Table.
    /// </summary>
    [DebuggerDisplay("{Type.Name}")]
    internal class TypeInfo
    {
        public TypeInfo(Type type, int rowNumber, TypeIdentifier typeIdentifier)
        {
            RowNumber = rowNumber;
            TypeIdentifier = typeIdentifier;
            Type = type;
        }

        /// <summary>
        /// Type from row.
        /// </summary>
        public Type Type { get; }
        
        /// <summary>
        /// Row number from Type.
        /// </summary>
        public int RowNumber { get; }
        
        /// <summary>
        /// TypeDef | TypeRef | TypeSpec.
        /// </summary>

        public TypeIdentifier TypeIdentifier { get; set; }
    }
}
