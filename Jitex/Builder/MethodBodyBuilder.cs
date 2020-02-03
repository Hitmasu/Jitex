using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.Builder.Exceptions;
using Jitex.PE;
using Jitex.PE.Signature;
using LocalVariableInfo = Jitex.PE.Signature.LocalVariableInfo;
using TypeInfo = Jitex.PE.TypeInfo;

namespace Jitex.Builder
{
    public class MethodBodyBuilder
    {
        /// <summary>
        /// Module from method body.
        /// </summary>
        public Module Module { get; set; }
        
        /// <summary>
        /// IL from body.
        /// </summary>
        public byte[] IL { get; set; }
        
        /// <summary>
        /// Local variables from method.
        /// </summary>
        public IList<LocalVariableInfo> LocalVariables { get; set; }
        
        /// <summary>
        /// If body contains some local variable.
        /// </summary>
        public bool HasLocalVariable => LocalVariables.Count > 0;

        /// <summary>
        /// Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        /// <param name="localVariables">Local variables of method.</param>
        /// <param name="module">Module from local variables.</param>
        public MethodBodyBuilder(byte[] il, IList<LocalVariableInfo> localVariables, Module module)
        {
            IL = il;
            LocalVariables = localVariables;
        }

        /// <summary>
        /// Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        public MethodBodyBuilder(byte[] il)
        {
            IL = il;
            LocalVariables = new List<LocalVariableInfo>();
        }

        /// <summary>
        /// Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignature()
        {
            List<byte> signature = new List<byte>
            {
                0x07, (byte) LocalVariables.Count
            };

            MetadataInfo metadataInfo = null;

            foreach (LocalVariableInfo localVariable in LocalVariables)
            {
                CorElementType elementType = localVariable.ElementType;

                if (elementType == CorElementType.ELEMENT_TYPE_CLASS)
                {
                    if (Module == null)
                        throw new ModuleNullException("Module can't be null with Local Variable of type Class ");
                    
                    if(metadataInfo == null)
                        metadataInfo = new MetadataInfo(Module.Assembly);
                    
                    TypeInfo typeInfo = metadataInfo.GetTypeInfo(localVariable.Type);

                    if (typeInfo == null)
                        throw new TypeNotFoundException($"{Module.Assembly.FullName} dont contains reference for type {localVariable.Type.Name}");

                    bool[] bitsRow = GetMinimalBits(typeInfo.RowNumber);
                    bool[] bitsTypeSpec = GetMinimalBits((int) typeInfo.TypeIdentifier);

                    byte[] compressedType = new byte[8];
                    bitsTypeSpec.CopyTo(compressedType, 0);
                    bitsRow.CopyTo(compressedType, 2);

                    signature.AddRange(compressedType);
                }
                else
                {
                    signature.Add((byte)elementType);
                }
            }

            signature.Insert(0, (byte) signature.Count);
            return signature.Cast<byte>().ToArray();
        }
        
        private static bool[] GetMinimalBits(int number)
        {
            BitArray bits = new BitArray(number);
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                if (bits[i])
                {
                    bool[] subBits = new bool[i];

                    for (int j = 0; j < i; j++)
                    {
                        subBits[j] = bits[j];
                    }

                    return subBits;
                }
            }

            return null;
        }
    }
}