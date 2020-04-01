using Jitex.Builder.Exceptions;
using Jitex.IL;
using Jitex.PE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Jitex.Utils.Extensions;

namespace Jitex.Builder
{
    public class MethodBody
    {
        /// <summary>
        /// Module from method body.
        /// </summary>
        public Module Module { get; set; }

        private byte[] _il;

        public byte[] IL
        {
            get => _il;
            set
            {
                _il = value;
                Operations = new ILReader(_il, Module);
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
        public bool HasLocalVariable => LocalVariables?.Count > 0;

        public uint MaxStackSize { get; set; }

        public MethodBody(MethodInfo methodBase)
        {
            Module = methodBase.Module;

            if (methodBase is DynamicMethod dynamicMethod)
            {
                _il = methodBase.GetILBytes();
                LocalVariables = new List<LocalVariableInfo>
                {
                    new LocalVariableInfo(typeof(long))
                };
                dynamicMethod.Invoke(null, null);
                Operations = new ILReader(dynamicMethod).ToList();
                MaxStackSize = 8;
            }
            else
            {
                IL = methodBase.GetILBytes();
                LocalVariables = methodBase.GetMethodBody().LocalVariables.Select(s => new LocalVariableInfo(s.LocalType)).ToList();
            }
        }

        public MethodBody(byte[] il, Module module, params Type[] variables)
        {
            Module = module;
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();
            IL = il;
        }

        /// <summary>
        /// Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        /// <param name="module">Module from body.</param>
        public MethodBody(byte[] il, Module module)
        {
            Module = module;
            IL = il;
        }

        /// <summary>
        /// Calculate .maxstack from body.
        /// </summary>
        private void CalculateMaxStack()
        {
            int maxStackSize = 0;
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
                    case StackBehaviour.Varpop:
                        break;

                    case StackBehaviour.Popref:
                    case StackBehaviour.Popi:
                    case StackBehaviour.Pop1:
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
                    MaxStackSize = (uint)maxStackSize;
                }
            }
        }

        /// <summary>
        /// Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignatureVariables()
        {
            BlobBuilder blob = new BlobBuilder();

            blob.WriteByte(0x07);
            blob.WriteCompressedInteger(LocalVariables.Count);

            foreach (LocalVariableInfo variable in LocalVariables)
            {
                CorElementType elementType = variable.ElementType;

                if (elementType == CorElementType.ELEMENT_TYPE_CLASS || elementType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    //TODO
                    //Pinned variable

                    if (Module == null)
                        throw new ModuleNullException("Module can't be null with a Local Variable of type Class ");

                    MetadataInfo metadataInfo = new MetadataInfo(variable.Type.Assembly);
                    EntityHandle typeHandle = metadataInfo.GetTypeHandle(variable.Type);

                    int typeInfo = CodedIndex.TypeDefOrRefOrSpec(typeHandle);

                    blob.WriteByte((byte)elementType);
                    blob.WriteCompressedInteger(typeInfo);
                }
                else
                {
                    blob.WriteByte((byte)elementType);
                }
            }

            BlobBuilder blobSize = new BlobBuilder();
            blobSize.WriteCompressedInteger(blob.Count);
            blob.LinkPrefix(blobSize);

            return blob.ToArray();
        }
    }
}
