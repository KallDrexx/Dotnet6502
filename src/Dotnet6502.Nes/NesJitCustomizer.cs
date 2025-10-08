using System.Reflection.Emit;
using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;

namespace Dotnet6502.Nes;

/// <summary>
/// Customizes the JIT operations for NES Roms
/// </summary>
public class NesJitCustomizer : IJitCustomizer
{
    private record IncrementCycleCount(int Cycles) : Ir6502.Instruction;

    private record CallDebugHook(string Info) : Ir6502.Instruction;

    public IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions)
    {
        var result = new List<ConvertedInstruction>();
        foreach (var instruction in instructions)
        {
            // Prepend cycle count instruction. This has to be done before the instruction's execution
            // otherwise it will get missed by branch/jump calls.
            // However, if this starts with a label instruction, that *must* come first so that
            // any loops retrigger cycles.
            var skipCount = instruction.Ir6502Instructions.FirstOrDefault() is Ir6502.Label ? 1 : 0;
            var updatedInstructions = new List<Ir6502.Instruction>();
            if (skipCount > 0)
            {
                updatedInstructions.Add(instruction.Ir6502Instructions[0]);
            }

            updatedInstructions.Add(new CallDebugHook(instruction.OriginalInstruction.ToString()));
            updatedInstructions.Add(new IncrementCycleCount(instruction.OriginalInstruction.Info.Cycles));
            updatedInstructions.AddRange(instruction.Ir6502Instructions.Skip(skipCount));

            var updatedInstruction = instruction with
            {
                Ir6502Instructions = updatedInstructions
            };

            result.Add(updatedInstruction);
        }

        return result;
    }

    public IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator> GetCustomIlGenerators()
    {
        return new Dictionary<Type, MsilGenerator.CustomIlGenerator>
        {
            { typeof(IncrementCycleCount), CreateCycleCountIlGenerator() },
            { typeof(CallDebugHook), CreateDebugHookIlGenerator() },
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

    private static MsilGenerator.CustomIlGenerator CreateDebugHookIlGenerator()
    {
        return (instruction, ilGenerator) =>
        {
            var debugInstruction = (CallDebugHook)instruction;

            // Load the hardware field
            ilGenerator.Emit(JitCompiler.LoadHalArg);

            // Cast from I6502Hal interface to NesHal concrete type
            ilGenerator.Emit(OpCodes.Castclass, typeof(NesHal));
            ilGenerator.Emit(OpCodes.Ldstr, debugInstruction.Info);

            var incrementMethod = typeof(NesHal).GetMethod(nameof(NesHal.DebugHook))!;
            ilGenerator.Emit(OpCodes.Callvirt, incrementMethod);
        };
    }
}