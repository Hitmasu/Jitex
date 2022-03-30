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
    public class ILReader : IEnumerable<Instruction>
    {
        /// <summary>
        /// Instructions IL.
        /// </summary>
        private readonly byte[] _il;

        /// <summary>
        /// Module token resolver.
        /// </summary>
        private readonly TokenResolver? _resolver;

        /// <summary>
        /// Generic class arguments used in instructions.
        /// </summary>
        private readonly Type[]? _genericTypeArguments;

        /// <summary>
        /// Generic method arguments used in instructions.
        /// </summary>
        private readonly Type[]? _genericMethodArguments;

        public ITokenResolver? CustomTokenResolver
        {
            get => _resolver?.CustomResolver;

            set
            {
                if (_resolver != null)
                    _resolver.CustomResolver = value;
            }
        }

        /// <summary>
        /// Read IL from method.
        /// </summary>
        /// <param name="method">Method to read IL.</param>
        public ILReader(MethodBase method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            _il = method.GetILBytes();

            if (method is not DynamicMethod && method is not ConstructorInfo)
            {
                _genericTypeArguments = method.DeclaringType!.GenericTypeArguments;
                _genericMethodArguments = method.GetGenericArguments();
            }

            _resolver = new TokenResolver(method);
        }

        /// <summary>
        /// Read IL from array byte.
        /// </summary>
        /// <param name="il">IL to read.</param>
        /// <param name="module">Module from IL.</param>
        /// <param name="genericTypeArguments">Generic class arguments used in instructions.</param>
        /// <param name="genericMethodArguments">Generic method arguments used in instructions.</param>
        public ILReader(byte[] il, Module? module, Type[]? genericTypeArguments = null, Type[]? genericMethodArguments = null)
        {
            _il = il;

            _genericTypeArguments = genericTypeArguments;
            _genericMethodArguments = genericMethodArguments;

            if (module != null)
                _resolver = new TokenResolver(module);
        }

        /// <summary>
        /// Get enumerator from reader.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<Instruction> GetEnumerator()
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
        private class ILEnumerator : IEnumerator<Instruction>
        {
            /// <summary>
            /// Instructions IL.
            /// </summary>
            private readonly byte[] _il;

            /// <summary>
            /// Module token resolver.
            /// </summary>
            private readonly TokenResolver? _resolver;

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
            private readonly Type[]? _genericTypeArguments;

            /// <summary>
            /// Generic method arguments used in instructions.
            /// </summary>
            private readonly Type[]? _genericMethodArguments;

            private readonly bool _isGeneric;

            /// <summary>
            ///     Current operation.
            /// </summary>
            public Instruction Current => ReadNextOperation();

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
            public ILEnumerator(byte[] il, TokenResolver? resolver, Type[]? genericTypeArguments, Type[]? genericMethodArguments)
            {
                _il = il;
                _resolver = resolver;
                _genericTypeArguments = genericTypeArguments;
                _genericMethodArguments = genericMethodArguments;

                _isGeneric = _genericMethodArguments is {Length: > 0} || _genericTypeArguments is {Length: > 0};
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
            private Instruction ReadNextOperation()
            {
                Instruction operation;

                int ilIndex = _position;

                short instruction = _il[_position++];

                if (instruction == 0xFE)
                    instruction = BitConverter.ToInt16(new[] {_il[_position++], (byte) instruction}, 0);

                OpCode opCode = Instruction.Translate(instruction);

                switch (opCode.OperandType)
                {
                    case OperandType.InlineI8:
                        operation = new Instruction(opCode, ReadInt64());
                        break;

                    case OperandType.InlineR:
                        operation = new Instruction(opCode, ReadDouble());
                        break;

                    case OperandType.InlineField:
                        (dynamic? Field, int Token) field = ReadField();
                        operation = new Instruction(opCode, field.Field, field.Token);
                        break;

                    case OperandType.InlineMethod:
                        (dynamic? Method, int Token) method = ReadMethod();
                        operation = new Instruction(opCode, method.Method, method.Token);
                        break;

                    case OperandType.InlineString:
                        (string String, int Token) @string = ReadString();
                        operation = new Instruction(opCode, @string.String, @string.Token);
                        break;

                    case OperandType.InlineType:
                        (dynamic? Type, int Token) type = ReadType();
                        operation = new Instruction(opCode, type.Type, type.Token);
                        break;

                    case OperandType.InlineI:
                        operation = new Instruction(opCode, ReadInt32());
                        break;

                    case OperandType.InlineSig:
                        (byte[] Signature, int Token) signature = ReadSignature();
                        operation = new Instruction(opCode, signature.Signature, signature.Token);
                        break;

                    case OperandType.InlineTok:
                        (dynamic? Member, int Token) member = ReadMember();
                        operation = new Instruction(opCode, member.Member, member.Token);
                        break;

                    case OperandType.InlineBrTarget:
                        operation = new Instruction(opCode, ReadInt32() + _position);
                        break;

                    case OperandType.ShortInlineR:
                        operation = new Instruction(opCode, ReadSingle());
                        break;

                    case OperandType.InlineVar:
                        operation = new Instruction(opCode, null);
                        _position += 2;
                        break;

                    case OperandType.ShortInlineBrTarget: //Repeat jump from original IL.
                    case OperandType.ShortInlineI:
                        if (opCode == OpCodes.Ldc_I4_S)
                            operation = new Instruction(opCode, (sbyte) ReadByte());
                        else
                            operation = new Instruction(opCode, ReadByte());
                        break;

                    case OperandType.ShortInlineVar:
                        operation = new Instruction(opCode, ReadByte());
                        break;

                    case OperandType.InlineSwitch:
                        int length = ReadInt32();
                        int[] branches = new int[length];

                        for (int i = 0; i < length; i++)
                            branches[i] = ReadInt32();

                        operation = new Instruction(opCode, branches);
                        break;

                    case OperandType.InlinePhi:
                        throw new NotImplementedException("[IL Reader] - OperandType.InlinePhi is not implemented!");

                    default:
                        operation = new Instruction(opCode, null);
                        break;
                }

                //Current position of operation
                operation.Index = _index++;

                //Current position in array byte
                operation.Offset = ilIndex;
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
            private (dynamic? Type, int Token) ReadType()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                Exception exception;

                try
                {
                    Type type = _resolver.ResolveType(token, _genericTypeArguments, _genericMethodArguments);
                    return (type, token);
                }
                catch (ArgumentException ex)
                {
                    exception = new TokenNotFoundException(token, ex);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                return (exception, token);
            }

            /// <summary>
            ///     Read <see cref="MethodInfo" /> reference from module.
            /// </summary>
            /// <returns><see cref="MethodInfo" /> referenced.</returns>
            private (dynamic? Method, int Token) ReadMethod()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                Exception exception;

                try
                {
                    MethodBase method;

                    if (_isGeneric)
                        method = _resolver.ResolveMethod(token, _genericTypeArguments, _genericMethodArguments);
                    else
                        method = _resolver.ResolveMethod(token);

                    return (method, token);
                }
                catch (ArgumentException ex)
                {
                    exception = new TokenNotFoundException(token, ex);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                return (exception, token);
            }

            /// <summary>
            ///     Read <see cref="FieldInfo" /> reference from module.
            /// </summary>
            /// <returns><see cref="FieldInfo" /> referenced.</returns>
            private (dynamic? Field, int Token) ReadField()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                Exception exception;

                try
                {
                    FieldInfo field;
                    
                    if (_isGeneric)
                        field = _resolver.ResolveField(token, _genericTypeArguments, _genericMethodArguments);
                    else
                        field = _resolver.ResolveField(token);

                    return (field, token);
                }
                catch (ArgumentException ex)
                {
                    exception = new TokenNotFoundException(token, ex);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                return (exception, token);
            }

            /// <summary>
            ///     Read <see cref="MemberInfo" /> reference from module.
            /// </summary>
            /// <returns></returns>
            private (dynamic? Member, int Token) ReadMember()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                Exception exception;

                try
                {
                    MemberInfo member;
                    if (_isGeneric)
                        member = _resolver.ResolveMember(token, _genericTypeArguments, _genericMethodArguments);
                    else
                        member = _resolver.ResolveMethod(token);

                    return (member, token);
                }
                catch (ArgumentException ex)
                {
                    exception = new TokenNotFoundException(token, ex);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                return (exception, token);
            }
            
            /// <summary>
            ///     Read <see cref="string" /> reference from module.
            /// </summary>
            /// <returns><see cref="string" /> referenced.</returns>
            private (string? String, int Token) ReadString()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                return (_resolver.ResolveString(token), token);
            }

            /// <summary>
            ///     Read Signature reference from module.
            /// </summary>
            /// <returns></returns>
            private (byte[]? Signature, int Token) ReadSignature()
            {
                int token = ReadInt32();

                if (_resolver == null)
                    return (null, token);

                byte[] signature = _resolver.ResolveSignature(token);
                return (signature, token);
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
                int value = BitConverter.ToInt32(_il, _position);
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