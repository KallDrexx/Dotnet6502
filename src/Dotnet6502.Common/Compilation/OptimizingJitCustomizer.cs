using Dotnet6502.Common.Hardware;
using System;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Performs an optimization pass on a set of 6502 IR instructions
public class OptimizingJitCustomizer<THal> : IJitCustomizer<THal> where THal : I6502Hal
{
    private record struct InstructionIndex(int Outer, int Inner);
    
    public void AddInstructions(Ir6502Interpreter interpreter)
    {
    }

    public IReadOnlyDictionary<Type, MsilGenerator<THal>.CustomIlGenerator> GetCustomIlGenerators()
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions)
    {
        // The index of a write instruction to a value that has not been read yet
        var openWrites = new Dictionary<Ir6502.Value, InstructionIndex>();
        var removedInstructionCount = 0;

        for (var outerIndex = 0; outerIndex < instructions.Count; outerIndex++)
        {
            var instruction = instructions[outerIndex];
            for (var innerIndex = 0; innerIndex < instruction.Ir6502Instructions.Count; innerIndex++)
            {
                // If we have any open writes marked, but we are reading from them, then
                // they are no longer open.
                var irInstruction = instruction.Ir6502Instructions[innerIndex];
                Ir6502.Value[] readValues = irInstruction switch
                {
                    Ir6502.Copy copy => [copy.Source],
                    Ir6502.Unary unary => [unary.Source],
                    Ir6502.Binary binary => [binary.Left, binary.Right],
                    Ir6502.JumpIfZero jumpIf => [jumpIf.Condition],
                    Ir6502.JumpIfNotZero jumpNot => [jumpNot.Condition],
                    Ir6502.PushStackValue push => [push.Source],
                    _ => [],
                };

                foreach (var value in readValues)
                {
                    openWrites.Remove(value);
                }

                // Pull out the value this instruction is writing to
                Ir6502.Value? writtenValue = irInstruction switch
                {
                    Ir6502.Copy copy => copy.Destination,
                    Ir6502.Unary unary => unary.Destination,
                    Ir6502.Binary binary => binary.Destination,
                    Ir6502.PopStackValue pop => pop.Destination,
                    _ => null,
                };

                if (writtenValue != null)
                {
                    // If we are writing to a value that was recently written to but not read to,
                    // then we can count the first write as a no-op, since it's never acted on.
                    //
                    // We only care about register and flags for now as we know writes to those are
                    // side effect free and are not used if not directly read in between. Memory mapping
                    // may mean that memory locations are read or acted on outside of the CPU, and thus
                    // we can't risk optimizing them out.

                    if (writtenValue is Ir6502.Register)
                    {
                        if (openWrites.TryGetValue(writtenValue, out var toRemove))
                        {
                            
                        }
                    }
                }
            }
        }

        return instructions;
    }
}
