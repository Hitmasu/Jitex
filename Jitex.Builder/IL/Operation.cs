using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Builder.IL
{
    /// <summary>
    /// Operation from a IL instruction.
    /// </summary>
    /// <remarks>
    /// An operation contains informations from an IL instruction.
    /// </remarks>
    [DebuggerDisplay("{OpCode} - {Instance}")]
    public partial class Operation
    {
        /// <summary>
        /// MetadataToken from instruction.
        /// </summary>
        public int? MetadataToken { get; }

        /// <summary>
        /// Index instruction.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Offset insruction.
        /// </summary>
        public int Offset { get; internal set; }

        /// <summary>
        /// Size operation (instruction length + value length)
        /// </summary>
        public int Size { get; internal set; }

        /// <summary>
        /// Operation Code IL.
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// Value from instruction.
        /// </summary>
        public dynamic Instance { get; set; }

        /// <summary>
        /// Create a new operation.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        /// <param name="instance">Value from instruction.</param>
        internal Operation(OpCode opCode, dynamic instance)
        {
            OpCode = opCode;
            Instance = instance;

            if (Instance is MemberInfo member)
            {
                MetadataToken = member.MetadataToken;
            }
        }

        /// <summary>
        /// Create a new operation.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        /// <param name="instance">Value from instruction.</param>
        /// <param name="metadataToken">MetadataToken from instruction.</param>
        internal Operation(OpCode opCode, dynamic instance, int metadataToken)
        {
            OpCode = opCode;
            Instance = instance;
            MetadataToken = metadataToken;
        }
    }

    /// <summary>
    /// Class helper to read IL instructions.
    /// </summary>
    public partial class Operation
    {
        /// <summary>
        ///     All Operation Codes.
        /// </summary>
        private static readonly IDictionary<short, OpCode> OpCodes;

        static Operation()
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
            return OpCodes[identifier];
        }
    }
}