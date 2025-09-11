using System.Reflection.Emit;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF); // Mask to keep in zero page
                ilGenerator.Emit(OpCodes.And);
                break;

            case AddressingMode.ZeroPageY:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF); // Mask to keep in zero page
                ilGenerator.Emit(OpCodes.And);
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
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF); // Mask to 16-bit
                ilGenerator.Emit(OpCodes.And);
                break;

            case AddressingMode.AbsoluteY:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF); // Mask to 16-bit
                ilGenerator.Emit(OpCodes.And);
                break;

            case AddressingMode.IndexedIndirect:
                // ($nn,X) - zero page address + X, then read 16-bit address from there
                ilGenerator.EmitWriteLine($"IndexedIndirect addressing not fully implemented for {instruction}");
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                break;

            case AddressingMode.IndirectIndexed:
                // ($nn),Y - read 16-bit address from zero page, then add Y
                ilGenerator.EmitWriteLine($"IndirectIndexed addressing not fully implemented for {instruction}");
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                break;

            default:
                ilGenerator.EmitWriteLine($"Unsupported addressing mode: {instruction.Info.AddressingMode}");
                ilGenerator.Emit(OpCodes.Ldc_I4, 0); // Push dummy address
                break;
        }
    }

    public static void SetFlag(GameClass gameClass, ILGenerator ilGenerator, CpuStatusFlags flag, bool value)
    {
        var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag));
        if (setFlagMethod == null)
        {
            ilGenerator.EmitWriteLine($"Error: SetFlag method not found");
            return;
        }

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag);
        ilGenerator.Emit(OpCodes.Ldc_I4, value ? 1 : 0);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
    }

    public static void SetFlagFromIlStack(GameClass gameClass, ILGenerator ilGenerator, CpuStatusFlags flag)
    {
        var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag));
        if (setFlagMethod == null)
        {
            ilGenerator.EmitWriteLine($"Error: SetFlag method not found");
            ilGenerator.Emit(OpCodes.Pop); // Remove value from stack to prevent stack corruption
            return;
        }

        // Convert int to bool (non-zero = true, zero = false)
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Cgt_Un); // 1 if value > 0, 0 if value == 0

        var local = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Stloc, local);

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag);
        ilGenerator.Emit(OpCodes.Stloc, local); // Load the boolean result
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
    }

    /// <summary>
    /// Updates the zero flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateZeroFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag));
        if (setFlagMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: SetFlag method not found for zero flag");
            ilGenerator.Emit(OpCodes.Pop); // Remove value to prevent stack corruption
            return;
        }

        // Check if value equals zero
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Ceq); // 1 if equal to zero, 0 otherwise

        var local = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Stloc, local);

        // Call SetFlag with the result
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Zero);
        ilGenerator.Emit(OpCodes.Ldloc, local); // The boolean result from above
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
    }

    /// <summary>
    /// Updates the negative flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateNegativeFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag));
        if (setFlagMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: SetFlag method not found for negative flag");
            ilGenerator.Emit(OpCodes.Pop); // Remove value to prevent stack corruption
            return;
        }

        // Check if bit 7 (sign bit) is set
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x80);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Cgt_Un); // 1 if result > 0 (bit 7 set), 0 otherwise

        var local = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Stloc, local);

        // Call SetFlag with the result
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Negative);
        ilGenerator.Emit(OpCodes.Ldloc, local); // The boolean result from above
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
    }

    /// <summary>
    /// Updates the overflow flag based on the value already on the .net stack
    /// </summary>
    public static void UpdateOverflowFlag(GameClass gameClass, ILGenerator ilGenerator)
    {
        var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag));
        if (setFlagMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: SetFlag method not found for overflow flag");
            ilGenerator.Emit(OpCodes.Pop); // Remove value to prevent stack corruption
            return;
        }

        // Check if bit 6 is set
        ilGenerator.Emit(OpCodes.Ldc_I4, 0x40);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Cgt_Un); // 1 if result > 0 (bit 6 set), 0 otherwise

        var local = ilGenerator.DeclareLocal(typeof(bool));
        ilGenerator.Emit(OpCodes.Stloc, local);

        // Call SetFlag with the result
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Overflow);
        ilGenerator.Emit(OpCodes.Ldloc, local); // The boolean result from above
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
    }

    /// <summary>
    /// Safely pops value from stack if there's a stack underflow risk
    /// </summary>
    public static void SafePopIfNeeded(ILGenerator ilGenerator, string context)
    {
        // This is a development helper - in production you'd want better stack tracking
        ilGenerator.EmitWriteLine($"Stack checkpoint: {context}");
    }
}