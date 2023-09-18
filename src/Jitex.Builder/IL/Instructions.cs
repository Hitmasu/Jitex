using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Jitex.Builder.IL
{
    public class Instructions : List<Instruction>
    {
        public new Instruction this[int index]
        {
            get => GetInstruction(index);
            set => SetInstruction(index, value);
        }

        public void Remove(int index)
        {
            ValidateIndex(index);

            RemoveAt(index);


            Instruction? prevInstruction = null;

            if (index > 0)
                prevInstruction = this.ElementAt(index - 1);

            foreach (var instruction in this.Skip(index))
            {
                if (prevInstruction != null)
                {
                    instruction.Index = prevInstruction.Index + 1;
                    instruction.Offset = prevInstruction.Offset + prevInstruction.Size;
                }

                prevInstruction = instruction;
            }
        }

        public void RemoveLast() => Remove(Count - 1);

        public void Add(Instruction instruction)
        {
            ValidateNotNull(instruction);

            if (Count > 0)
            {
                var lastInstruction = this.Last();
                instruction.Index = lastInstruction.Index + 1;
                instruction.Offset = lastInstruction.Offset + lastInstruction.Size;
            }

            base.Add(instruction);
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

        public Instruction GetInstruction(int index)
        {
            ValidateIndex(index);
            return base[index];
        }

        public void SetInstruction(int index, Instruction instruction)
        {
            ValidateIndex(index);
            ValidateNotNull(instruction);

            base[index] = instruction;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateIndex(int index)
        {
            if (index > Count - 1 || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index),
                    "Index was out of range. Must be non-negative and less than the size of the collection.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateNotNull(Instruction? instruction)
        {
            if (instruction == null)
                throw new ArgumentNullException(nameof(instruction), "Instruction cannot be null.");
        }

        public IEnumerable<byte> ToBytes()
        {
            return this.SelectMany(instruction => instruction.ToBytes());
        }

        public static implicit operator byte[](Instructions instructions)
        {
            return instructions.ToBytes().ToArray();
        }
    }
}