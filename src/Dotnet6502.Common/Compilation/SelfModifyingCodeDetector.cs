using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Detects self-modifying code patterns during decompilation analysis.
/// </summary>
public static class SelfModifyingCodeDetector
{
    /// <summary>
    /// Identifies common self-modifying patterns by looking for memory writes that target
    /// instruction bytes within the same decompiled function (e.g. GETCHR-style operand patching).
    /// This is intentionally conservative and focuses on high-impact patterns.
    /// </summary>
    public static bool TryDetect(DecompiledFunction function, out IReadOnlyList<ushort> affectedAddresses)
    {
        var instructionBytes = GetInstructionBytes(function);
        var detected = new HashSet<ushort>();

        foreach (var instruction in function.OrderedInstructions)
        {
            if (!IsMemoryWriteInstruction(instruction))
            {
                continue;
            }

            if (!TryGetTargetAddress(instruction, out var targetAddress, out var usesIndexingOrIndirect))
            {
                continue;
            }

            if (instructionBytes.Contains(targetAddress) ||
                (usesIndexingOrIndirect && instructionBytes.Contains((ushort)(targetAddress + 1))))
            {
                detected.Add(targetAddress);
            }
        }

        affectedAddresses = detected.OrderBy(x => x).ToArray();
        return affectedAddresses.Count > 0;
    }

    private static bool IsMemoryWriteInstruction(DisassembledInstruction instruction)
    {
        if (instruction.Info.AddressingMode is AddressingMode.Implied or AddressingMode.Accumulator)
        {
            return false;
        }

        return instruction.Info.Type switch
        {
            InstructionType.Store => true,
            InstructionType.Increment => true,
            InstructionType.Decrement => true,
            _ => false,
        };
    }

    private static bool TryGetTargetAddress(
        DisassembledInstruction instruction,
        out ushort address,
        out bool usesIndexingOrIndirect)
    {
        usesIndexingOrIndirect = false;
        address = 0;

        if (instruction.Bytes == null || instruction.Operands.Length == 0)
        {
            return false;
        }

        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.ZeroPage:
                address = instruction.Operands[0];
                return true;

            case AddressingMode.ZeroPageX:
            case AddressingMode.ZeroPageY:
            case AddressingMode.IndexedIndirect:
            case AddressingMode.IndirectIndexed:
                usesIndexingOrIndirect = true;
                address = instruction.Operands[0];
                return true;

            case AddressingMode.Absolute:
                if (instruction.Operands.Length < 2)
                {
                    return false;
                }

                address = (ushort)(instruction.Operands[0] | (instruction.Operands[1] << 8));
                return true;

            case AddressingMode.AbsoluteX:
            case AddressingMode.AbsoluteY:
                usesIndexingOrIndirect = true;
                if (instruction.Operands.Length < 2)
                {
                    return false;
                }

                address = (ushort)(instruction.Operands[0] | (instruction.Operands[1] << 8));
                return true;

            default:
                return false;
        }
    }

    private static HashSet<ushort> GetInstructionBytes(DecompiledFunction function)
    {
        var instructionBytes = new HashSet<ushort>();
        foreach (var instruction in function.OrderedInstructions)
        {
            if (instruction.SubAddressOrder != 0)
            {
                continue;
            }

            for (var offset = 0; offset < instruction.Info.Size; offset++)
            {
                instructionBytes.Add((ushort)(instruction.CPUAddress + offset));
            }
        }

        return instructionBytes;
    }
}