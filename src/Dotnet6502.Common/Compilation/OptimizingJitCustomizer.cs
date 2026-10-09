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
        return new Dictionary<Type, MsilGenerator<THal>.CustomIlGenerator>();
    }

    public IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions)
    {
        // The index of a write instruction to a value that has not been read yet
        var openWrites = new Dictionary<Ir6502.Value, InstructionIndex>();
        var instructionsToRemove = new List<InstructionIndex>();

        var instructionCount = instructions.SelectMany(x => x.Ir6502Instructions).Count();

        List<Ir6502.Flag> flags = [
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            new Ir6502.Flag(Ir6502.FlagName.Zero),
            new Ir6502.Flag(Ir6502.FlagName.InterruptDisable),
            new Ir6502.Flag(Ir6502.FlagName.BFlag),
            new Ir6502.Flag(Ir6502.FlagName.Decimal),
            new Ir6502.Flag(Ir6502.FlagName.Overflow),
            new Ir6502.Flag(Ir6502.FlagName.Negative),
        ];

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
                    if (openWrites.TryGetValue(value, out var previous))
                    {
                        Console.WriteLine($"Write to {value} has been read");
                    }
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

                    if (writtenValue is Ir6502.AllFlags)
                    {
                        // We are overwriting all the flags, so previous writes can be removed
                        foreach (var flag in flags)
                        {
                            if (openWrites.TryGetValue(flag, out var prevValue))
                            {
                                var prevInstruction = instructions[prevValue.Outer].Ir6502Instructions[prevValue.Inner];
                                Console.WriteLine($"Removing open write to {flag}");
                                instructionsToRemove.Add(prevValue);
                                openWrites.Remove(flag);
                            }
                        }

                        if (openWrites.TryGetValue(writtenValue, out var prevAllFlags))
                        {
                            var prevInstruction = instructions[prevAllFlags.Outer].Ir6502Instructions[prevAllFlags.Inner];
                            Console.WriteLine($"Removing open write to {writtenValue}");
                            instructionsToRemove.Add(prevAllFlags);
                        }

                        Console.WriteLine($"Writing to {writtenValue}");
                        openWrites[writtenValue] = new InstructionIndex(outerIndex, innerIndex);

                    }
                    else if (writtenValue is Ir6502.Flag)
                    {
                        // A previous open write to all flags is no longer considered open, since we don't know
                        // if other flags are relevant.
                        openWrites.Remove(new Ir6502.AllFlags());

                        if (openWrites.TryGetValue(writtenValue, out var prevValue))
                        {
                            var prevInstruction = instructions[prevValue.Outer].Ir6502Instructions[prevValue.Inner];
                            Console.WriteLine($"Removing open write to {writtenValue}");
                            instructionsToRemove.Add(prevValue);
                        }

                        Console.WriteLine($"Writing to {writtenValue}");
                        openWrites[writtenValue] = new InstructionIndex(outerIndex, innerIndex);
                    }
                    else if (writtenValue is Ir6502.Register)
                    {
                        if (openWrites.TryGetValue(writtenValue, out var prevValue))
                        {
                            var prevInstruction = instructions[prevValue.Outer].Ir6502Instructions[prevValue.Inner];
                            Console.WriteLine($"Removing open write to {writtenValue}");
                            instructionsToRemove.Add(prevValue);
                        }

                        Console.WriteLine($"Writing to {writtenValue}");
                        openWrites[writtenValue] = new InstructionIndex(outerIndex, innerIndex);
                    }
                }
            }
        }

        // Remove all instructions flagged for removal
        var orderedRemovals = instructionsToRemove.OrderByDescending(x => x.Outer).ThenByDescending(x => x.Inner).ToArray();
        foreach (var removal in orderedRemovals)
        {
            instructions[removal.Outer].Ir6502Instructions.RemoveAt(removal.Inner);
        }

        return instructions;
    }
}
