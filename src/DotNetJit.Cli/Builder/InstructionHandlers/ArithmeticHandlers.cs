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
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.XIndex);

                UpdateZeroFlag(gameClass.Registers.XIndex, gameClass, ilGenerator);
                UpdateNegativeFlag(gameClass.Registers.XIndex, gameClass, ilGenerator);
                break;

           case "INY": // Increment Y by 1
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.YIndex);

                UpdateZeroFlag(gameClass.Registers.YIndex, gameClass, ilGenerator);
                UpdateNegativeFlag(gameClass.Registers.YIndex, gameClass, ilGenerator);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private static void UpdateZeroFlag(FieldInfo register, GameClass gameClass, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldsfld, register);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0);
        ilGenerator.Emit(OpCodes.Ceq);

        IlUtils.SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Zero);
    }

    private static void UpdateNegativeFlag(FieldInfo register, GameClass gameClass, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldsfld, register);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x80); // check msb
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x80);
        ilGenerator.Emit(OpCodes.Ceq);

        IlUtils.SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Negative);
    }
}