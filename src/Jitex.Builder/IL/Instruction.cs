using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Builder.Utils;

namespace Jitex.Builder.IL
{
    /// <summary>
    /// Instruction from a IL instruction.
    /// </summary>
    /// <remarks>
    /// An instruction contains informations from an IL instruction.
    /// </remarks>
    public partial class Instruction
    {
        private readonly Type? _type;
        private bool _shouldUpdateBytes = true;
        private dynamic? _value;
        private byte[]? _bytes;

        public byte[] Bytes => ToBytes();

        /// <summary>
        /// MetadataToken from instruction.
        /// </summary>
        public int? MetadataToken { get; }

        /// <summary>
        /// Index from instruction (index from array of instructions).
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Offset from insruction (index from array of bytes).
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        /// Size instruction (opcode length + value length)
        /// </summary>
        public int Size => (OpCode.Size == 1 ? 1 : 2) + OperandSize();

        /// <summary>
        /// Operation Code IL.
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// Value from instruction.
        /// </summary>
        public dynamic? Value
        {
            get => _value;
            set
            {
                _value = value;
                _shouldUpdateBytes = true;
            }
        }

        /// <summary>
        /// Create a new instruction.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        public Instruction(OpCode opCode)
        {
            OpCode = opCode;
        }

        /// <summary>
        /// Create a new instruction.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        /// <param name="value">Value from instruction.</param>
        internal Instruction(OpCode opCode, object? value)
        {
            OpCode = opCode;
            Value = value;

            if (value != null)
                _type = value.GetType();

            if (Value is MemberInfo member)
                MetadataToken = member.MetadataToken;
        }

        /// <summary>
        /// Create a new instruction.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        /// <param name="value">Value from instruction.</param>
        /// <param name="metadataToken">MetadataToken from instruction.</param>
        internal Instruction(OpCode opCode, object? value, int metadataToken)
        {
            OpCode = opCode;
            Value = value;
            MetadataToken = metadataToken;

            if (value != null)
                _type = value.GetType();
        }

        private int OperandSize()
        {
            switch (OpCode.OperandType)
            {
                case OperandType.InlineI8:
                    return sizeof(long);

                case OperandType.InlineR:
                    return sizeof(double);

                case OperandType.InlineBrTarget:
                case OperandType.InlineTok:
                case OperandType.InlineSig:
                case OperandType.InlineI:
                case OperandType.InlineType:
                case OperandType.InlineString:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSwitch:
                    return sizeof(int);

                case OperandType.ShortInlineR:
                    return sizeof(float);

                case OperandType.InlineVar:
                    return sizeof(short);

                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                    return sizeof(byte);

                case OperandType.InlineNone:
                    return 0;

                default:
                    throw new NotImplementedException($"OperandType {OpCode.OperandType} not implemented.");
            }
        }

        public byte[] ToBytes()
        {
            if (!_shouldUpdateBytes)
                return _bytes!;

            List<byte> bytes = new(10);

            if (OpCode.Size == 1)
                bytes.Add((byte)OpCode.Value);
            else
                bytes.AddRange(BitConverter.GetBytes(OpCode.Value).Reverse());

            if (MetadataToken.HasValue)
            {
                bytes.AddRange(BitConverter.GetBytes(MetadataToken.Value));
            }
            else
            {
                int size = OperandSize();

                if (size > 0)
                {
                    if (_value == null)
                        throw new ArgumentNullException($"Value to be written is null. Opcode: {OpCode.Name}");

                    byte[] buffer = ByteHelper.GetBytes(_value);
                    bytes.AddRange(buffer.Take(size));
                }
            }


            _bytes = bytes.ToArray();
            _shouldUpdateBytes = false;
            return _bytes;
        }
    }

    /// <summary>
    /// Class helper to read IL instructions.
    /// </summary>
    public partial class Instruction
    {
        /// <summary>
        ///     All Operation Codes.
        /// </summary>
        private static readonly IDictionary<short, OpCode> OpCodes;

        static Instruction()
        {
            OpCodes = new Dictionary<short, OpCode>();
            LoadOpCodes();
        }

        /// <summary>
        ///     Load all operation codes.
        /// </summary>
        private static void LoadOpCodes()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                OpCode opCode = (OpCode)field.GetValue(null);
                OpCodes.Add(opCode.Value, opCode);
            }
        }

        /// <summary>
        ///     Get <see cref="OpCode" /> from instruction.
        /// </summary>
        /// <param name="identifier">Instruction IL.</param>
        /// <returns>Operation code of instruction.</returns>
        public static OpCode Translate(short identifier)
        {
            if (OpCodes.TryGetValue(identifier, out OpCode opcode))
                return opcode;

            throw new KeyNotFoundException($"OpCode {identifier} not found");
        }

        public override string ToString()
        {
            string? value = "";

            if (Value is Exception ex)
                value = ex.Message;
            else
                value = Value?.ToString();

            return $"{OpCode.Name} {value}";
        }
    }
}