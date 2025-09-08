using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles arithmetic based 6502 instructions
/// </summary>
public class ArithmeticHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["INX", "INY"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "INX": // increment X register by 1
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.XIndex);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

           case "INY": // Increment Y by 1
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Dup); // For zero flag
                ilGenerator.Emit(OpCodes.Dup); // For negative flag
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.YIndex);

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}