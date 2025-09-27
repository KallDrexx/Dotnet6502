using System.Reflection.Emit;
using Dotnet6502.Common;

namespace Dotnet6502.Nes;

/// <summary>
/// Customizes the JIT operations for NES Roms
/// </summary>
public class NesJitCustomizer : IJitCustomizer
{
    private record IncrementCycleCount(int Cycles) : Ir6502.Instruction;

    public IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions)
    {
        var result = new List<ConvertedInstruction>();
        foreach (var instruction in instructions)
        {
            // Prepend cycle count instruction. This has to be done before the instruction's execution
            // otherwise it will get missed by branch/jump calls.
            var updatedInstruction = instruction with
            {
                Ir6502Instructions = instruction.Ir6502Instructions
                    .Prepend(new IncrementCycleCount(instruction.OriginalInstruction.Info.Cycles))
                    .ToArray(),
            };

            result.Add(updatedInstruction);
        }

        return result;
    }

    public IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator> GetCustomIlGenerators()
    {
        return new Dictionary<Type, MsilGenerator.CustomIlGenerator>
        {
            { typeof(IncrementCycleCount), CreateCycleCountIlGenerator() }
        };
    }

    private static MsilGenerator.CustomIlGenerator CreateCycleCountIlGenerator()
    {
        return (instruction, ilGenerator) =>
        {
            if (instruction is IncrementCycleCount incrementCycleCount)
            {
                // Load the hardware field
                ilGenerator.Emit(JitCompiler.LoadHalArg);

                // Cast from I6502Hal interface to NesHal concrete type
                ilGenerator.Emit(OpCodes.Castclass, typeof(NesHal));

                // Load the cycle count as a constant
                ilGenerator.Emit(OpCodes.Ldc_I4, incrementCycleCount.Cycles);

                // Call NesHal.IncrementCpuCycleCount(int count)
                var incrementMethod = typeof(NesHal).GetMethod(nameof(NesHal.IncrementCpuCycleCount))!;
                ilGenerator.Emit(OpCodes.Callvirt, incrementMethod);
            }
            else
            {
                throw new NotSupportedException(instruction.GetType().FullName);
            }
        };
    }
}