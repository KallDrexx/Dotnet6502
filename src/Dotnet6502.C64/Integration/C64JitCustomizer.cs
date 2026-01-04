using System.Reflection.Emit;
using Dotnet6502.C64.Hardware;
using Dotnet6502.Common.Compilation;

namespace Dotnet6502.C64.Integration;

/// <summary>
/// Customizes the JIT operations for the C64
/// </summary>
public class C64JitCustomizer : IJitCustomizer
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
            //
            // TODO: This probably needs to just be part of the base JIT system, I'm assuming any
            // 6502 system is going to need it.
            var skipCount = instruction.Ir6502Instructions.FirstOrDefault() is Ir6502.Label ? 1 : 0;
            var updatedInstructions = new List<Ir6502.Instruction>();
            if (skipCount > 0)
            {
                updatedInstructions.Add(instruction.Ir6502Instructions[0]);
            }

            updatedInstructions.Add(new IncrementCycleCount(instruction.OriginalInstruction.Info.Cycles));
            updatedInstructions.Add(new CallDebugHook(instruction.OriginalInstruction.ToString()));
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
            if (instruction is not IncrementCycleCount incrementCycleCount)
            {
                throw new NotSupportedException(instruction.GetType().FullName);
            }

            // Load the hardware field
            ilGenerator.Emit(JitCompiler.LoadHalArg);

            // Cast from I6502Hal interface to NesHal concrete type
            ilGenerator.Emit(OpCodes.Castclass, typeof(C64Hal));

            // Load the cycle count as a constant
            ilGenerator.Emit(OpCodes.Ldc_I4, incrementCycleCount.Cycles);

            // Call NesHal.IncrementCpuCycleCount(int count)
            var incrementMethod = typeof(C64Hal).GetMethod(nameof(C64Hal.IncrementCpuCycleCount))!;
            ilGenerator.Emit(OpCodes.Callvirt, incrementMethod);
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
            ilGenerator.Emit(OpCodes.Castclass, typeof(C64Hal));
            ilGenerator.Emit(OpCodes.Ldstr, debugInstruction.Info);

            var debugHook = typeof(C64Hal).GetMethod(nameof(C64Hal.DebugHook))!;
            ilGenerator.Emit(OpCodes.Callvirt, debugHook);
        };
    }
}