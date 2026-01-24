using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Common.Compilation;

public static class JitOptimizer
{
    public static IReadOnlyList<ConvertedInstruction> Optimize(
        IReadOnlyList<ConvertedInstruction> input,
        MemoryBus memoryBus)
    {
        var mutated = input.ToList();
        OptimizeDynamicLoadOperand(mutated, memoryBus);

        return mutated;
    }

    private static void OptimizeDynamicLoadOperand(List<ConvertedInstruction> input, MemoryBus memoryBus)
    {
        // If we see a memory copy into a memory, and that memory address corresponds to
        // an instruction's operand, then we need that instruction's operand to get the value
        // from memory instead of it being hardcoded into the IR instruction.
        for (var x = 0; x < input.Count; x++)
        {
            var inputInstruction = input[x];
            var copyInstructions = inputInstruction.Ir6502Instructions.OfType<Ir6502.Copy>();
            foreach (var copyInstruction in copyInstructions)
            {
                if (copyInstruction.Destination is not Ir6502.Memory destinationMemory)
                {
                    continue;
                }

                // We can only do this optimization if it's a zero page or absolute memory address.
                // If a register is being added than we can't reliably predict if this modifies our
                // own instructions.
                if (destinationMemory.RegisterToAdd != null)
                {
                    continue;
                }

                if (destinationMemory.Address > 0xFF)
                {
                    continue; // Only zero page supported atm
                }

                // Are any instructions using this address?
                for (var y = 0; y < input.Count; y++)
                {
                    var comparerInstruction = input[y];
                    var comparerOriginal = comparerInstruction.OriginalInstruction;

                    if (comparerOriginal.CPUAddress < destinationMemory.Address &&
                        comparerOriginal.CPUAddress + comparerOriginal.Info.Size >= destinationMemory.Address)
                    {
                        var updatedInstructions =
                            new List<Ir6502.Instruction>(comparerInstruction.Ir6502Instructions.Count);

                        // Does any IR instruction reference this value?
                        foreach (var innerIr in comparerInstruction.Ir6502Instructions)
                        {
                            switch (innerIr)
                            {
                                case Ir6502.Copy(var source, var dest):
                                {
                                    if (source is Ir6502.Memory sourceMemory &&
                                        sourceMemory.RegisterToAdd == null)
                                    {
                                        source = new Ir6502.IndirectMemory((byte)destinationMemory.Address, false, false);
                                    }

                                    if (dest is Ir6502.Memory destMemory &&
                                        destMemory.RegisterToAdd == null)
                                    {
                                        dest = new Ir6502.IndirectMemory((byte)destinationMemory.Address, false, false);
                                    }

                                    updatedInstructions.Add(new Ir6502.Copy(source, dest));
                                    break;
                                }

                                default:
                                    updatedInstructions.Add(innerIr);
                                    break;
                            }
                        }

                        input[y] = comparerInstruction with { Ir6502Instructions = updatedInstructions };
                    }
                }
            }
        }
    }
}