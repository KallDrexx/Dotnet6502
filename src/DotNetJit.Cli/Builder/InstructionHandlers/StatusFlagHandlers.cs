using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles instructions related to status flags
/// </summary>
public class StatusFlagHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["SEI", "CLD"];

    protected override void HandleInternal(
        ILGenerator ilGenerator,
        DisassembledInstruction instruction,
        GameClass gameClass)
    {

        switch (instruction.Info.Mnemonic)
        {
            case "SEI":
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.InterruptDisable, true);
                break;

            case "CLD":
                IlUtils.SetFlag(gameClass, ilGenerator, CpuStatusFlags.Decimal, false);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}