using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handlers for op codes that deal with the stack pointer
/// </summary>
public class StackHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["TXS"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "TXS":
                // Copy the value at the X register to the
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.StackPointer);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}