using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.IL;
using Jitex.Builder.IL.Resolver;
using Jitex.Builder.Utils.Extensions;

namespace Jitex.Builder.Method
{
    /// <summary>
    /// Provides create a body of a method.
    /// </summary>
    public class MethodBody
    {
        private uint _maxStackSize;

        /// <summary>
        /// Original method.
        /// </summary>
        public MethodBase? Method { get; }

        /// <summary>
        /// Module from body.
        /// </summary>
        public Module? Module { get; }

        /// <summary>
        /// Generic class arguments used in body.
        /// </summary>
        public Type[]? GenericTypeArguments { get; set; }

        /// <summary>
        /// Generic 
        /// </summary>
        public Type[]? GenericMethodArguments { get; set; }

        public ITokenResolver? CustomTokenResolver { get; set; }

        /// <summary>
        /// IL from body.
        /// </summary>
        public byte[] IL { get; set; }

        /// <summary>
        ///     Local variables from method.
        /// </summary>
        public IList<LocalVariableInfo> LocalVariables { get; set; }

        /// <summary>
        /// If body contains some local variable.
        /// </summary>
        public bool HasLocalVariable => LocalVariables?.Count > 0;

        /// <summary>
        /// Stack size from body.
        /// </summary>
        public uint MaxStackSize
        {
            get
            {
                if (_maxStackSize == 0)
                    CalculateIL();

                return _maxStackSize;
            }

            set => _maxStackSize = value;
        }

        /// <summary>
        /// Exceptions handle count.
        /// </summary>
        public uint EHCount { get; set; }

        /// <summary>
        /// Create a body from method.
        /// </summary>
        /// <param name="methodBase">Method to read.</param>
        public MethodBody(MethodBase methodBase)
        {
            Method = methodBase;
            Module = methodBase.Module;

            if (methodBase is not DynamicMethod)
            {
                if (methodBase.IsGenericMethod)
                    GenericMethodArguments = methodBase.GetGenericArguments();

                if (methodBase.DeclaringType!.IsGenericType)
                    GenericTypeArguments = methodBase.DeclaringType.GetGenericArguments();
            }

            LocalVariables = methodBase.GetMethodBody().LocalVariables.Select(s => new LocalVariableInfo(s.LocalType!, s.IsPinned)).ToList();
            IL = methodBase.GetILBytes();
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in body.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in body.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, Module? module, Type[]? genericTypeArguments = null, Type[]? genericMethodArguments = null, params Type[] variables)
        {
            Module = module;
            LocalVariables = variables.Select(s => new LocalVariableInfo(s)).ToList();
            GenericTypeArguments = genericTypeArguments;
            GenericMethodArguments = genericMethodArguments;

            IL = il.ToArray();
        }


        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in body.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in body.</param>
        public MethodBody(IEnumerable<byte> il, Module? module, Type[]? genericTypeArguments = null, Type[]? genericMethodArguments = null) : this(il, module, genericTypeArguments, genericMethodArguments, new Type[0])
        {
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, Module? module, params Type[] variables) : this(il, module, null, null, variables)
        {
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="variables">Local variables.</param>
        public MethodBody(IEnumerable<byte> il, params Type[] variables) : this(il, null, variables)
        {
        }

        /// <summary>
        /// Create a body from IL.
        /// </summary>
        /// <param name="il">IL instructions.</param>
        /// <param name="maxStack">Stack size to body.</param>
        public MethodBody(IEnumerable<byte> il, uint maxStack = 8)
        {
            IL = il.ToArray();
            MaxStackSize = maxStack;
            LocalVariables = Array.Empty<LocalVariableInfo>();
        }

        /// <summary>
        /// Read IL instructions from body.
        /// </summary>
        /// <returns>Operations from body.</returns>
        public ILReader ReadIL()
        {
            ILReader reader;

            if (Method != null)
                reader = new(Method);
            else
                reader = new(IL, Module, GenericTypeArguments, GenericMethodArguments);

            reader.CustomTokenResolver = CustomTokenResolver;

            return reader;
        }

        /// <summary>
        ///     Calculate .maxstack and EHCount from body.
        /// </summary>
        private void CalculateIL()
        {
            int maxStackSize = 0;
            uint highMaxStack = 0;

            foreach (Instruction operation in ReadIL())
            {
                maxStackSize += CalculateMaxStack(operation.OpCode);

                if (maxStackSize > highMaxStack)
                    highMaxStack = (uint) maxStackSize;

                if (operation.OpCode == OpCodes.Leave || operation.OpCode == OpCodes.Leave_S)
                    EHCount++;
            }

            MaxStackSize = highMaxStack;
        }

        private int CalculateMaxStack(OpCode opcode)
        {
            int maxStackSize = 0;
            switch (opcode.StackBehaviourPush)
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
                    throw new NotImplementedException($"Stack operation not implemented: {opcode.Name}");
            }

            switch (opcode.StackBehaviourPop)
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
                    throw new NotImplementedException($"Stack operation not implemented: {opcode.Name}");
            }

            return maxStackSize;
        }

        /// <summary>
        ///     Get compressed signature from local variables.
        /// </summary>
        /// <returns>Byte array - compressed signature.</returns>
        public byte[] GetSignatureVariables()
        {
            SignatureHelper signatureHelper = SignatureHelper.GetLocalVarSigHelper();

            foreach (LocalVariableInfo variable in LocalVariables)
                signatureHelper.AddArgument(variable.Type, variable.IsPinned);

            byte[] blobSignature = signatureHelper.GetSignature();
            byte[] signature = new byte[blobSignature.Length + 1];

            Array.Copy(blobSignature, 0, signature, 1, blobSignature.Length);
            signature[0] = (byte) blobSignature.Length;

            return signature;
        }
    }
}