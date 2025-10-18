namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Adds standard debugging string and polling detection instructions to all instruction paths
/// </summary>
public class StandardJitCustomizer : IJitCustomizer
{
    public IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions)
    {
        var result = new List<ConvertedInstruction>();

        foreach (var instruction in instructions)
        {
            if (instruction.Ir6502Instructions.Count == 0)
            {
                result.Add(instruction);
                continue;
            }

            var newIrInstructions = new List<Ir6502.Instruction>();

            var debugInstruction = new Ir6502.StoreDebugString(instruction.OriginalInstruction.ToString());
            var pollInstruction = new Ir6502.PollForInterrupt(instruction.OriginalInstruction.CPUAddress);

            // Make sure the label stays as the first instruction if it starts with a label, in case of loops.
            var firstInstructionIsLabel = instruction.Ir6502Instructions[0] is Ir6502.Label;
            if (firstInstructionIsLabel)
            {
                newIrInstructions.Add(instruction.Ir6502Instructions[0]);
            }

            newIrInstructions.Add(debugInstruction);
            newIrInstructions.Add(pollInstruction);
            newIrInstructions.AddRange(instruction.Ir6502Instructions.Skip(firstInstructionIsLabel ? 1 : 0));

            result.Add(instruction with {Ir6502Instructions = newIrInstructions});
        }

        return result;
    }

    public IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator> GetCustomIlGenerators()
    {
        return new Dictionary<Type, MsilGenerator.CustomIlGenerator>();
    }
}