using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Jitex.Builder.IL
{
    public class Instructions : IEnumerable<Instruction>
    {
        private readonly List<Instruction> _operations;

        public Instructions()
        {
            _operations = new List<Instruction>();
        }

        public IEnumerator<Instruction> GetEnumerator()
        {
            return _operations.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Remove(int index) => _operations.RemoveAt(index);
        public void RemoveLast() => Remove(_operations.Count - 1);

        public void Add(Instruction instruction) => _operations.Add(instruction);
        public void Add(OpCode opcode, object? value) => Add(new Instruction(opcode, value));
        public void AddRange(IEnumerable<Instruction> instruction) => _operations.AddRange(instruction);

        public IEnumerable<byte> ToBytes()
        {
            return _operations.SelectMany(instruction => instruction.ToBytes());
        }

        public static implicit operator byte[](Instructions instructions)
        {
            return instructions.ToBytes().ToArray();
        }
    }
}