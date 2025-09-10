using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles all processor flag manipulation instructions
/// Merged from StatusFlagHandlers and ProcessorFlagHandlers
/// </summary>
public class ProcessorFlagHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["CLC", "CLD", "CLI", "CLV", "SEC", "SED", "SEI"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "CLC": // Clear carry flag
                ilGenerator.EmitWriteLine("Clear carry flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Carry, false);
                break;

            case "CLD": // Clear decimal flag
                ilGenerator.EmitWriteLine("Clear decimal flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Decimal, false);
                break;

            case "CLI": // Clear interrupt disable flag
                ilGenerator.EmitWriteLine("Clear interrupt disable flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.InterruptDisable, false);
                break;

            case "CLV": // Clear overflow flag
                ilGenerator.EmitWriteLine("Clear overflow flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Overflow, false);
                break;

            case "SEC": // Set carry flag
                ilGenerator.EmitWriteLine("Set carry flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Carry, true);
                break;

            case "SED": // Set decimal flag
                ilGenerator.EmitWriteLine("Set decimal flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Decimal, true);
                break;

            case "SEI": // Set interrupt disable flag
                ilGenerator.EmitWriteLine("Set interrupt disable flag");
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.InterruptDisable, true);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}