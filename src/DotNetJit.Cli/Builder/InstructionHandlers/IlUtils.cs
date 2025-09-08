using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

public static class IlUtils
{
    public static void LoadAddressToStack(
        DisassembledInstruction instruction,
        GameClass gameClass,
        ILGenerator ilGenerator)
    {
        int tempAddress;
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.ZeroPage:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                break;

            case AddressingMode.ZeroPageX:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegisters.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.ZeroPageY:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegisters.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.Absolute:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                break;

            case AddressingMode.AbsoluteX:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegisters.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.AbsoluteY:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegisters.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }
    }

    public static void SetFlag(GameClass gameClass, ILGenerator ilGenerator, CpuStatusFlags flag, bool value)
    {
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag);
        ilGenerator.Emit(OpCodes.Ldc_I4, value ? 1 : 0);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod!);
    }
}