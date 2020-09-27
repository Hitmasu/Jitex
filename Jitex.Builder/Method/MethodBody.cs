using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Jitex.Builder.IL;
using Jitex.Builder.Method.Exceptions;
using Jitex.Builder.PE;
using Jitex.Builder.Utils.Extensions;

namespace Jitex.Builder.Method
{
    public class MethodBody
    {
        private byte[] _il;

        /// <summary>
        ///     Module from method body.
        /// </summary>
        public Module Module { get; }

        public Type[] GenericTypeArguments { get; set; }
        public Type[] GenericMethodArguments { get; set; }

        public byte[] IL
        {
            get => _il;
            set
            {
                _il = value;
                CalculateMaxStack();
            }
        }

        /// <summary>
        ///     Local variables from method.
        /// </summary>
        public IList<LocalVariableInfo> LocalVariables { get; set; }

        /// <summary>
        ///     If body contains some local variable.
        /// </summary>
        public bool HasLocalVariable => LocalVariables?.Count > 0;

        public uint MaxStackSize { get; set; }

        public MethodBody(MethodBase methodBase)
        {
            Module = methodBase.Module;
            LocalVariables = methodBase.GetMethodBody().LocalVariables.Select(s => new LocalVariableInfo(s.LocalType)).ToList();

            if (methodBase.IsGenericMethod)
                GenericMethodArguments = methodBase.GetGenericArguments();

            if (methodBase.DeclaringType.IsGenericType)
                GenericTypeArguments = methodBase.DeclaringType.GetGenericArguments();

            IL = methodBase.GetILBytes();
        }

        public MethodBody(byte[] il, Module module, Type[] genericTypeArguments = null, Type[] genericMethodArguments = null, params Type[] variables)
        {
            Module = module;
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;

            IL = il;
        }

        /// <summary>
        ///     Create a new method body.
        /// </summary>
        /// <param name="il">IL of method.</param>
        /// <param name="module">Module from body.</param>
        public MethodBody(byte[] il, Module module, Type[] genericTypeArguments = null, Type[] genericMethodArguments = null)
        {
            Module = module;
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;

            IL = il;
        }

        public IEnumerable<Operation> ReadIL()
        {
            return new ILReader(IL, Module, GenericTypeArguments, GenericMethodArguments);
        }

        /// <summary>
        ///     Calculate .maxstack from body.
        /// </summary>
        private void CalculateMaxStack()
        {
            int maxStackSize = 0;
            foreach (Operation operation in ReadIL())
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
                        throw new NotImplementedException($"Stack operation not implemented: {operation.OpCode.Name}");
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
                    case StackBehaviour.Popref_popi_pop1:
                    case StackBehaviour.Popref_popi_popi:
                    case StackBehaviour.Popref_popi_popi8:
                    case StackBehaviour.Popref_popi_popr4:
                    case StackBehaviour.Popref_popi_popr8:
                    case StackBehaviour.Popref_popi_popref:
                        maxStackSize -= 3;
                        break;

                    default:
                        throw new NotImplementedException($"Stack operation not implemented: {operation.OpCode.Name}");
                }

                if (maxStackSize > MaxStackSize)
                {
                    MaxStackSize = (uint)maxStackSize;
                }
            }
        }

        /// <summary>
        ///     Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignatureVariables()
        {
            BlobBuilder blob = new BlobBuilder();

            blob.WriteByte(0x07);
            blob.WriteCompressedInteger(LocalVariables.Count);

            MetadataInfo medatataModule = new MetadataInfo(Module.Assembly);

            foreach (LocalVariableInfo variable in LocalVariables)
            {
                CorElementType elementType = variable.ElementType;

                if (elementType == CorElementType.ELEMENT_TYPE_SZARRAY)
                {
                    blob.WriteByte((byte)elementType);
                    elementType = LocalVariableInfo.DetectCorElementType(variable.Type.GetElementType());
                }

                if (elementType == CorElementType.ELEMENT_TYPE_CLASS || elementType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                {
                    //TODO
                    //Pinned variables

                    if (Module == null)
                        throw new ModuleNullException("Module can't be null with a Local Variable of type Class");

                    EntityHandle typeHandle = medatataModule.GetTypeHandle(variable.Type);

                    //Check if type was already referenced on metadata from module
                    //If not, we should get reference in assembly of type.
                    //Ex.: String
                    if (typeHandle == default)
                    {
                        MetadataInfo metadataAssembly = new MetadataInfo(variable.Type.Assembly);
                        typeHandle = metadataAssembly.GetTypeHandle(variable.Type);
                    }

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