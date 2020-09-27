using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Jitex.Builder.IL
{
    [DebuggerDisplay("{OpCode} - {Instance}")]
    public partial class Operation
    {
        public int? MetadataToken { get; }

        public int Index { get; internal set; }
        public int ILIndex { get; internal set; }

        public int Size { get; internal set; }

        /// <summary>
        ///     Operation Code IL.
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        ///     Instance value of operation.
        /// </summary>
        public dynamic Instance { get; set; }

        /// <summary>
        ///     Create new operation.
        /// </summary>
        /// <param name="opCode">Operation Code IL.</param>
        /// <param name="instance">Operation value instance.</param>
        public Operation(OpCode opCode, dynamic instance)
        {
            OpCode = opCode;
            Instance = instance;

            if (Instance is MemberInfo member)
            {
                MetadataToken = member.MetadataToken;
            }
        }

        public Operation(OpCode opCode, dynamic instance, int metadataToken)
        {
            OpCode = opCode;
            Instance = instance;
            MetadataToken = metadataToken;
        }
    }

    public partial class Operation
    {
        private static readonly object LockState = new object();

        /// <summary>
        ///     All Operation Codes.
        /// </summary>
        private static IDictionary<short, OpCode> _opCodes;

        /// <summary>
        ///     Load all operation codes.
        /// </summary>
        private static void LoadOpCodes()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (FieldInfo field in fields)
            {
                OpCode opCode = (OpCode) field.GetValue(null);
                _opCodes.Add(opCode.Value, opCode);
            }
        }

        /// <summary>
        ///     Get <see cref="OpCode" /> from instruction.
        /// </summary>
        /// <param name="identifier">Instruction IL.</param>
        /// <returns>Operation code of instruction.</returns>
        public static OpCode Translate(short identifier)
        {
            lock (LockState)
            {
                if (_opCodes == null)
                {
                    _opCodes = new Dictionary<short, OpCode>();
                    LoadOpCodes();
                }
            }

            return _opCodes[identifier];
        }
    }
}