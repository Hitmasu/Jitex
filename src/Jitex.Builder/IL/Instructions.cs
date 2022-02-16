using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Jitex.Builder.IL
{
    public class Instructions : IEnumerable<Instruction>
    {
        private readonly List<Instruction> _instructions = new(10);

        public Instruction this[int index]
        {
            get => GetInstruction(index);
            set => SetInstruction(index, value);
        }

        public IEnumerator<Instruction> GetEnumerator()
        {
            return _instructions.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Remove(int index)
        {
            ValidateIndex(index);

            _instructions.RemoveAt(index);

            Instruction? prevInstruction = null;

            if (index > 0)
                prevInstruction = _instructions.ElementAt(index - 1);

            foreach (Instruction instruction in _instructions.Skip(index))
            {
                if (prevInstruction != null)
                {
                    instruction.Index = prevInstruction.Index + 1;
                    instruction.Offset = prevInstruction.Offset + prevInstruction.Size;
                }

                prevInstruction = instruction;
            }
        }

        public void RemoveLast() => Remove(_instructions.Count - 1);

        public void Add(Instruction instruction)
        {
            ValidateNotNull(instruction);

            if (_instructions.Count > 0)
            {
                Instruction lastInstruction = _instructions.Last();
                instruction.Index = lastInstruction.Index + 1;
                instruction.Offset = lastInstruction.Offset + lastInstruction.Size;
            }

            _instructions.Add(instruction);
        }

        public Instruction Add(OpCode opcode)
        {
            Instruction instruction = new(opcode);
            Add(instruction);
            return instruction;
        }

        public Instruction Add(OpCode opcode, object? value)
        {
            Instruction instruction = new(opcode, value);
            Add(instruction);
            return instruction;
        }

        public void AddRange(IEnumerable<Instruction> instructions)
        {
            foreach (Instruction instruction in instructions)
                Add(instruction);
        }

        public Instruction GetInstruction(int index)
        {
            ValidateIndex(index);

            return _instructions[index];
        }

        public void SetInstruction(int index, Instruction instruction)
        {
            ValidateIndex(index);
            ValidateNotNull(instruction);

            _instructions[index] = instruction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(int index)
        {
            if (index > _instructions.Count - 1 || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateNotNull(Instruction? instruction)
        {
            if (instruction == null)
                throw new ArgumentNullException(nameof(instruction), "Instruction cannot be null.");
        }

        public IEnumerable<byte> ToBytes()
        {
            return _instructions.SelectMany(instruction => instruction.ToBytes());
        }

        public static implicit operator byte[](Instructions instructions)
        {
            return instructions.ToBytes().ToArray();
        }
    }
}