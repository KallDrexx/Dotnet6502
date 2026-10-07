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

        for (var outerIndex = 0; outerIndex < instructions.Count; outerIndex++)
        {
            var instruction = instructions[outerIndex];
            for (var innerIndex = 0; innerIndex < instruction.Ir6502Instructions.Count; innerIndex++)
            {
                var irInstruction = instruction.Ir6502Instructions[innerIndex];
                if (irInstruction is Ir6502.Copy copy)
                {
                    
                }
            }
        }

        return instructions;
    }
}
