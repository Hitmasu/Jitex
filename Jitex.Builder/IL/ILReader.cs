using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Jitex.Builder.Exceptions;
using Jitex.Builder.IL.Resolver;
using Jitex.Builder.Utils.Extensions;

namespace Jitex.Builder.IL
{
    /// <summary>
    /// MSIL reader.
    /// </summary>
    /// <remarks>
    /// Read MSIL instructions from array byte or method.
    /// </remarks>
    public class ILReader : IEnumerable<Operation>
    {
        /// <summary>
        /// Instructions IL.
        /// </summary>
        private readonly byte[] _il;

        /// <summary>
        /// Module token resolver.
        /// </summary>
        private readonly ITokenResolver _resolver;

        /// <summary>
        /// Generic class arguments used in instructions.
        /// </summary>
        private readonly Type[] _genericTypeArguments;

        /// <summary>
        /// Generic method arguments used in instructions.
        /// </summary>
        private readonly Type[] _genericMethodArguments;

        /// <summary>
        /// Read IL from method.
        /// </summary>
        /// <param name="methodILBase">Method to read IL.</param>
        public ILReader(MethodBase methodILBase)
        {
            if (methodILBase == null)
                throw new ArgumentNullException(nameof(methodILBase));

            _il = methodILBase.GetILBytes();

            _genericTypeArguments = methodILBase.DeclaringType.GenericTypeArguments;
            _genericMethodArguments = methodILBase.GetGenericArguments();

            if (methodILBase is DynamicMethod dynamicMethod)
                _resolver = new DynamicMethodTokenResolver(dynamicMethod);
            else
                _resolver = new ModuleTokenResolver(methodILBase.Module);
        }

        /// <summary>
        /// Read IL from array byte.
        /// </summary>
        /// <param name="il">IL to read.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in instructions.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in instructions.</param>
        public ILReader(byte[] il, Module module, Type[] genericTypeArguments = null, Type[] genericMethodArguments = null)
        {
            _il = il;

            _genericTypeArguments = genericTypeArguments;
            _genericMethodArguments = genericMethodArguments;

            if (module != null)
                _resolver = new ModuleTokenResolver(module);
        }

        /// <summary>
        /// Get enumerator from reader.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Operation> GetEnumerator()
        {
            return new ILEnumerator(_il, _resolver, _genericTypeArguments, _genericMethodArguments);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerator to read instructions.
        /// </summary>
        private class ILEnumerator : IEnumerator<Operation>
        {
            /// <summary>
            /// Instructions IL.
            /// </summary>
            private readonly byte[] _il;

            /// <summary>
            /// Module token resolver.
            /// </summary>
            private readonly ITokenResolver _resolver;

            /// <summary>
            /// Index from instructions.
            /// </summary>
            private int _index;

            /// <summary>
            /// Current position of read.
            /// </summary>
            private int _position;

            /// <summary>
            /// Generic class arguments used in instructions.
            /// </summary>
            private readonly Type[] _genericTypeArguments;

            /// <summary>
            /// Generic method arguments used in instructions.
            /// </summary>
            private readonly Type[] _genericMethodArguments;

            /// <summary>
            ///     Current operation.
            /// </summary>
            public Operation Current => ReadNextOperation();

            /// <summary>
            /// Current operation.
            /// </summary>
            object IEnumerator.Current => Current;

            /// <summary>
            /// Create a new enumerator to read instructions.
            /// </summary>
            /// <param name="il">IL to read.</param>
            /// <param name="resolver">Module to resolver tokens.</param>
            /// <param name="genericTypeArguments">Generic class arguments used in instructions.</param>
            /// <param name="genericMethodArguments">Generic method arguments used in instructions.</param>
            public ILEnumerator(byte[] il, ITokenResolver resolver, Type[] genericTypeArguments, Type[] genericMethodArguments)
            {
                _il = il;
                _resolver = resolver;
                _genericTypeArguments = genericTypeArguments;
                _genericMethodArguments = genericMethodArguments;
            }

            public void Dispose()
            {
                //throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                return _position < _il.Length;
            }

            /// <summary>
            /// Read next operation from IL.
            /// </summary>
            /// <returns>The next operation.</returns>
            private Operation ReadNextOperation()
            {
                Operation operation = null;

                int ilIndex = _position;

                short instruction = _il[_position++];

                if (instruction == 0xFE)
                    instruction = BitConverter.ToInt16(new[] { _il[_position++], (byte)instruction }, 0);

                OpCode opCode = Operation.Translate(instruction);

                switch (opCode.OperandType)
                {
                    case OperandType.InlineI8:
                        operation = new Operation(opCode, ReadInt64());
                        break;

                    case OperandType.InlineR:
                        operation = new Operation(opCode, ReadDouble());
                        break;

                    case OperandType.InlineField:
                        (FieldInfo Field, int Token) field = ReadField();
                        operation = new Operation(opCode, field.Field, field.Token);
                        break;

                    case OperandType.InlineMethod:
                        (MethodBase Method, int Token) method = ReadMethod();
                        operation = new Operation(opCode, method.Method, method.Token);
                        break;

                    case OperandType.InlineString:
                        (string String, int Token) @string = ReadString();
                        operation = new Operation(opCode, @string.String, @string.Token);
                        break;

                    case OperandType.InlineType:
                        (Type Type, int Token) type = ReadType();
                        operation = new Operation(opCode, type.Type, type.Token);
                        break;

                    case OperandType.InlineI:
                        operation = new Operation(opCode, ReadInt32());
                        break;

                    case OperandType.InlineSig:
                        (byte[] Signature, int Token) signature = ReadSignature();
                        operation = new Operation(opCode, signature.Signature, signature.Token);
                        break;

                    case OperandType.InlineTok:
                        (MemberInfo Member, int Token) member = ReadMember();
                        operation = new Operation(opCode, member.Member, member.Token);
                        break;

                    case OperandType.InlineBrTarget:
                        operation = new Operation(opCode, ReadInt32() + _position);
                        break;

                    case OperandType.ShortInlineR:
                        operation = new Operation(opCode, ReadSingle());
                        break;

                    case OperandType.InlineVar:
                        _position += 2;
                        break;

                    case OperandType.ShortInlineBrTarget: //Repeat jump from original IL.
                    case OperandType.ShortInlineI:
                        if (opCode == OpCodes.Ldc_I4_S)
                            operation = new Operation(opCode, (sbyte)ReadByte());
                        else
                            operation = new Operation(opCode, ReadByte());
                        break;

                    case OperandType.ShortInlineVar:
                        operation = new Operation(opCode, ReadByte());
                        break;

                    case OperandType.InlineSwitch:
                        int length = ReadInt32();
                        int[] branches = new int[length];

                        for (int i = 0; i < length; i++)
                            branches[i] = ReadInt32();

                        operation = new Operation(opCode, branches);
                        break;

                    case OperandType.InlinePhi:
                        throw new NotImplementedException("[IL Reader] - OperandType.InlinePhi is not implemented!");

                    default:
                        operation = new Operation(opCode, null);
                        break;
                }

                //Current position of operation
                operation.Index = _index++;

                //Current position in array byte
                operation.Offset = ilIndex;

                //Size bytes of operation
                operation.Size = _position - ilIndex;
                return operation;
            }

            public void Reset()
            {
                _position = 0;
            }

            #region ReadTypes

            /// <summary>
            ///     Read <see cref="Type" /> reference from module.
            /// </summary>
            /// <returns><see cref="Type" /> referenced.</returns>
            private (Type Type, int Token) ReadType()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                Type type;

                if (_resolver is ModuleTokenResolver)
                    type = _resolver.ResolveType(token, _genericTypeArguments, _genericMethodArguments);
                else
                    type = _resolver.ResolveType(token);

                return (type, token);
            }

            /// <summary>
            ///     Read <see cref="string" /> reference from module.
            /// </summary>
            /// <returns><see cref="string" /> referenced.</returns>
            private (string String, int Token) ReadString()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                return (_resolver.ResolveString(token), token);
            }

            /// <summary>
            ///     Read <see cref="MethodInfo" /> reference from module.
            /// </summary>
            /// <returns><see cref="MethodInfo" /> referenced.</returns>
            private (MethodBase Method, int Token) ReadMethod()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                MethodBase method;

                if (_resolver is ModuleTokenResolver)
                    method = _resolver.ResolveMethod(token, _genericTypeArguments, _genericMethodArguments);
                else
                    method = _resolver.ResolveMethod(token);

                return (method, token);
            }

            /// <summary>
            ///     Read <see cref="FieldInfo" /> reference from module.
            /// </summary>
            /// <returns><see cref="FieldInfo" /> referenced.</returns>
            private (FieldInfo Field, int Token) ReadField()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                FieldInfo field;

                if (_resolver is ModuleTokenResolver)
                    field = _resolver.ResolveField(token, _genericTypeArguments, _genericMethodArguments);
                else
                    field = _resolver.ResolveField(token);

                return (field, token);
            }

            /// <summary>
            ///     Read Signature reference from module.
            /// </summary>
            /// <returns></returns>
            private (byte[] Signature, int Token) ReadSignature()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                byte[] signature = _resolver.ResolveSignature(token);
                return (signature, token);
            }

            /// <summary>
            ///     Read <see cref="MemberInfo" /> reference from module.
            /// </summary>
            /// <returns></returns>
            private (MemberInfo Member, int Token) ReadMember()
            {
                if (_resolver == null)
                    throw new ModuleNullException();

                int token = ReadInt32();

                MemberInfo member;

                if (_resolver is ModuleTokenResolver)
                    member = _resolver.ResolveMember(token, _genericTypeArguments, _genericMethodArguments);
                else
                    member = _resolver.ResolveMember(token);

                return (member, token);
            }

            /// <summary>
            ///     Read <see cref="long" /> value.
            /// </summary>
            /// <returns><see cref="long" /> value.</returns>
            private long ReadInt64()
            {
                long value = BitConverter.ToInt64(_il, _position);
                _position += 8;
                return value;
            }

            /// <summary>
            ///     Read <see cref="int" /> value.
            /// </summary>
            /// <returns><see cref="int" /> value.</returns>
            private int ReadInt32()
            {
                int value = BitConverter.ToInt32(_il,_position);
                _position += 4;
                return value;
            }

            private double ReadSingle()
            {
                float value = BitConverter.ToSingle(_il, _position);
                _position += 4;
                return value;
            }

            /// <summary>
            ///     Read <see cref="double" /> value.
            /// </summary>
            /// <returns><see cref="double" /> value.</returns>
            private double ReadDouble()
            {
                double value = BitConverter.ToDouble(_il, _position);
                _position += 8;
                return value;
            }

            /// <summary>
            ///     Read <see cref="byte" /> value.
            /// </summary>
            /// <returns><see cref="byte" /> value.</returns>
            private byte ReadByte()
            {
                return _il[_position++];
            }

            #endregion
        }
    }
}