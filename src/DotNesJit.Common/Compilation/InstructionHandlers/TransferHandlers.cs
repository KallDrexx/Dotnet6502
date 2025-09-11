using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

/// <summary>
/// Handles register transfer instructions
/// </summary>
public class TransferHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["TAX", "TAY", "TXA", "TYA", "TSX", "TXS"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "TAX": // Transfer Accumulator to X
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.XIndex);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "TAY": // Transfer Accumulator to Y
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.YIndex);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "TXA": // Transfer X to Accumulator
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "TYA": // Transfer Y to Accumulator
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "TSX": // Transfer Stack Pointer to X
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.StackPointer);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.XIndex);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "TXS": // Transfer X to Stack Pointer (no flags affected)
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.StackPointer);
                break;

            default:
                ilGenerator.EmitWriteLine($"Unimplemented transfer instruction: {instruction.Info.Mnemonic}");
                break;
        }
    }
}