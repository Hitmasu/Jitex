using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.Exceptions;
using Jitex.Builder.IL;
using Jitex.PE;

using TypeInfo = Jitex.PE.TypeInfo;

namespace Jitex.Builder
{
    public class MethodBody
    {
        private byte[] _il;

        /// <summary>
        /// Module from method body.
        /// </summary>
        public Module Module { get; set; }

        /// <summary>
        /// IL from body.
        /// </summary>
        public byte[] IL
        {

            get => _il;

            set
            {
                _il = value;
                ReadOperations();
                CalculateMaxStack();
            }
        }

        /// <summary>
        /// Operations from IL.
        /// </summary>
        public IEnumerable<Operation> Operations { get; private set; }

        /// <summary>
        /// Local variables from method.
        /// </summary>
        public IList<LocalVariableInfo> LocalVariables { get; set; }

        /// <summary>
        /// If body contains some local variable.
        /// </summary>
        public bool HasLocalVariable => LocalVariables.Count > 0;

        public uint MaxStackSize { get; private set; }

        /// <summary>
        /// Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        /// <param name="localVariables">Local variables of method.</param>
        /// <param name="module">Module from body.</param>
        public MethodBody(byte[] il, IList<LocalVariableInfo> localVariables, Module module)
        {
            LocalVariables = localVariables;
            Module = module;
            IL = il;
        }

        /// <summary>
        /// Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        /// <param name="module">Module from body.</param>
        public MethodBody(byte[] il, Module module)
        {
            IL = il;
            Module = module;
        }

        private void ReadOperations()
        {
            ILReader reader = new ILReader(_il, Module);
            Operations = reader;
        }

        /// <summary>
        /// Calculate .maxstack from body.
        /// </summary>
        private void CalculateMaxStack()
        {
            uint maxStackSize = 0;

            foreach (Operation operation in Operations)
            {
                switch (operation.OpCode.StackBehaviourPush)
                {
                    case StackBehaviour.Push0:
                        break;

                    case StackBehaviour.Push1:
                    case StackBehaviour.Pushi:
                    case StackBehaviour.Pushi8:
                    case StackBehaviour.Pushr4:
                    case StackBehaviour.Pushr8:
                    case StackBehaviour.Pushref:
                    case StackBehaviour.Varpush:
                        maxStackSize++;
                        break;

                    case StackBehaviour.Push1_push1:
                        maxStackSize += 2;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                switch (operation.OpCode.StackBehaviourPop)
                {
                    case StackBehaviour.Pop0:
                        break;

                    case StackBehaviour.Popref:
                    case StackBehaviour.Popi:
                    case StackBehaviour.Pop1:
                    case StackBehaviour.Varpop:
                        maxStackSize--;
                        break;

                    case StackBehaviour.Popi_popr4:
                    case StackBehaviour.Popi_popr8:
                    case StackBehaviour.Popref_pop1:
                    case StackBehaviour.Popref_popi:
                    case StackBehaviour.Pop1_pop1:
                    case StackBehaviour.Popi_popi:
                    case StackBehaviour.Popi_pop1:
                    case StackBehaviour.Popi_popi8:
                        maxStackSize -= 2;
                        break;

                    case StackBehaviour.Popi_popi_popi:
                    case StackBehaviour.Popref_popi_popi:
                    case StackBehaviour.Popref_popi_popi8:
                    case StackBehaviour.Popref_popi_popr4:
                    case StackBehaviour.Popref_popi_popr8:
                    case StackBehaviour.Popref_popi_popref:
                        maxStackSize -= 3;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                if (maxStackSize > MaxStackSize)
                {
                    MaxStackSize = maxStackSize;
                }
            }
        }

        /// <summary>
        /// Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignatureVariables()
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

                    if (metadataInfo == null)
                        metadataInfo = new MetadataInfo(Module.Assembly);

                    TypeInfo typeInfo = metadataInfo.GetTypeInfo(localVariable.Type);

                    if (typeInfo == null)
                        throw new TypeNotFoundException($"{Module.Assembly.FullName} dont contains reference for type {localVariable.Type.Name}");

                    bool[] bitsRow = GetMinimalBits(typeInfo.RowNumber);
                    bool[] bitsTypeSpec = GetMinimalBits((int)typeInfo.TypeIdentifier);

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

            signature.Insert(0, (byte)signature.Count);
            return signature.Cast<byte>().ToArray();
        }

        /// <summary>
        /// Get bits from integer.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private static bool[] GetMinimalBits(int number)
        {
            BitArray bits = new BitArray(number);
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                if (bits[i])
                {
                    bool[] subBits = new bool[i];

                    for (int j = 0; j <= i; j++)
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
