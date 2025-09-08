using System.Reflection;
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
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.ZeroPageY:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.Absolute:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                break;

            case AddressingMode.AbsoluteX:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.AbsoluteY:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
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

    public static void SetFlagFromIlStack(GameClass gameClass, ILGenerator ilGenerator, CpuStatusFlags flag)
    {
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));

        // Store the current value to a local variable so we can get it in the right order
        var local = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Stloc, local);

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag);
        ilGenerator.Emit(OpCodes.Ldloc, local);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod!);
    }

    /// <summary>
    /// Updates the zero flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateZeroFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, 0);
        ilGenerator.Emit(OpCodes.Ceq);

        SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Zero);
    }

    /// <summary>
    /// Updates the negative flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateNegativeFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x80); // check msb
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x80);
        ilGenerator.Emit(OpCodes.Ceq);

        SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Negative);
    }

    /// <summary>
    /// Updates the overflow flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateOverflowFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x40); // check msb
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x40);
        ilGenerator.Emit(OpCodes.Ceq);

        SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Overflow);
    }
}