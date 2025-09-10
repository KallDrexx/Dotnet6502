using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles arithmetic based 6502 instructions - COMPLETE VERSION
/// </summary>
public class ArithmeticHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["INX", "INY", "DEX", "DEY", "ADC", "SBC", "AND", "ORA", "EOR", "INC", "DEC"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "INX": // increment X register by 1
                HandleRegisterIncrement(ilGenerator, gameClass.Registers.XIndex, gameClass, "X");
                break;

            case "INY": // Increment Y by 1
                HandleRegisterIncrement(ilGenerator, gameClass.Registers.YIndex, gameClass, "Y");
                break;

            case "DEX": // Decrement X by 1
                HandleRegisterDecrement(ilGenerator, gameClass.Registers.XIndex, gameClass, "X");
                break;

            case "DEY": // Decrement Y by 1
                HandleRegisterDecrement(ilGenerator, gameClass.Registers.YIndex, gameClass, "Y");
                break;

            case "ADC": // Add with Carry
                HandleADC(ilGenerator, instruction, gameClass);
                break;

            case "SBC": // Subtract with Carry
                HandleSBC(ilGenerator, instruction, gameClass);
                break;

            case "AND": // Logical AND
                HandleLogicalAND(ilGenerator, instruction, gameClass);
                break;

            case "ORA": // Logical OR
                HandleLogicalORA(ilGenerator, instruction, gameClass);
                break;

            case "EOR": // Exclusive OR
                HandleLogicalEOR(ilGenerator, instruction, gameClass);
                break;

            case "INC": // Increment Memory
                HandleMemoryIncrement(ilGenerator, instruction, gameClass);
                break;

            case "DEC": // Decrement Memory
                HandleMemoryDecrement(ilGenerator, instruction, gameClass);
                break;

            default:
                ilGenerator.EmitWriteLine($"Unimplemented arithmetic instruction: {instruction.Info.Mnemonic}");
                break;
        }
    }

    private void HandleRegisterIncrement(ILGenerator ilGenerator, FieldInfo register, GameClass gameClass, string regName)
    {
        // Load current register value
        ilGenerator.Emit(OpCodes.Ldsfld, register);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF); // Mask to byte
        ilGenerator.Emit(OpCodes.And);

        // Store back to register and prepare for flag updates
        ilGenerator.Emit(OpCodes.Dup); // For zero flag
        ilGenerator.Emit(OpCodes.Dup); // For negative flag
        ilGenerator.Emit(OpCodes.Stsfld, register);

        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleRegisterDecrement(ILGenerator ilGenerator, FieldInfo register, GameClass gameClass, string regName)
    {
        // Load current register value
        ilGenerator.Emit(OpCodes.Ldsfld, register);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF); // Mask to byte (handles underflow)
        ilGenerator.Emit(OpCodes.And);

        // Store back to register and prepare for flag updates
        ilGenerator.Emit(OpCodes.Dup); // For zero flag
        ilGenerator.Emit(OpCodes.Dup); // For negative flag
        ilGenerator.Emit(OpCodes.Stsfld, register);

        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleADC(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));
        var getFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetFlag));

        if (setFlagMethod == null || getFlagMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: SetFlag or GetFlag method not found for ADC");
            return;
        }

        var resultLocal = ilGenerator.DeclareLocal(typeof(int));
        var carryLocal = ilGenerator.DeclareLocal(typeof(int));
        var compareLocal = ilGenerator.DeclareLocal(typeof(bool));

        // Load accumulator
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

        // Load operand
        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
        }
        else
        {
            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            if (readMemoryMethod != null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }

        // Load carry flag value
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Carry);
        ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);

        // Convert bool to int (0 or 1)
        var noCarryLabel = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brfalse, noCarryLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Br, continueLabel);
        ilGenerator.MarkLabel(noCarryLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.MarkLabel(continueLabel);

        ilGenerator.Emit(OpCodes.Stloc, carryLocal);

        // Add all three values: A + operand + carry
        ilGenerator.Emit(OpCodes.Add); // A + operand
        ilGenerator.Emit(OpCodes.Ldloc, carryLocal);
        ilGenerator.Emit(OpCodes.Add); // + carry
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // Set carry flag if result > 255
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
        ilGenerator.Emit(OpCodes.Cgt);
        ilGenerator.Emit(OpCodes.Stloc, compareLocal);

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Carry);
        ilGenerator.Emit(OpCodes.Ldloc, compareLocal);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);

        // Mask result to 8 bits and store in accumulator
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

        // Update zero and negative flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleSBC(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));
        var getFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetFlag));

        if (setFlagMethod == null || getFlagMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: SetFlag or GetFlag method not found for SBC");
            return;
        }

        var resultLocal = ilGenerator.DeclareLocal(typeof(int));
        var borrowLocal = ilGenerator.DeclareLocal(typeof(int));
        var compareLocal = ilGenerator.DeclareLocal(typeof(bool));

        // Load accumulator
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

        // Load operand
        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
        }
        else
        {
            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            if (readMemoryMethod != null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }

        // Load carry flag (inverted for borrow: 0 = borrow, 1 = no borrow)
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Carry);
        ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);

        // Convert bool to int and invert for borrow
        var carrySetLabel = ilGenerator.DefineLabel();
        var borrowContinueLabel = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brtrue, carrySetLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_1); // Carry clear = borrow 1
        ilGenerator.Emit(OpCodes.Br, borrowContinueLabel);
        ilGenerator.MarkLabel(carrySetLabel);
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // Carry set = borrow 0
        ilGenerator.MarkLabel(borrowContinueLabel);

        ilGenerator.Emit(OpCodes.Stloc, borrowLocal);

        // Subtract: A - operand - borrow
        ilGenerator.Emit(OpCodes.Sub); // A - operand
        ilGenerator.Emit(OpCodes.Ldloc, borrowLocal);
        ilGenerator.Emit(OpCodes.Sub); // - borrow
        ilGenerator.Emit(OpCodes.Stloc, resultLocal);

        // Set carry flag if no borrow occurred (result >= 0)
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Clt); // 1 if result < 0, 0 if result >= 0
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor); // Invert: 0 if result < 0, 1 if result >= 0
        ilGenerator.Emit(OpCodes.Stloc, compareLocal);

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.Carry);
        ilGenerator.Emit(OpCodes.Ldloc, compareLocal);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);

        // Mask result to 8 bits and store in accumulator
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

        // Update zero and negative flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleLogicalAND(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        // Load accumulator
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

        // Load operand
        LoadOperandForLogical(ilGenerator, instruction, gameClass);

        // Perform AND operation
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

        // Update flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleLogicalORA(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        // Load accumulator
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

        // Load operand
        LoadOperandForLogical(ilGenerator, instruction, gameClass);

        // Perform OR operation
        ilGenerator.Emit(OpCodes.Or);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

        // Update flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleLogicalEOR(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        // Load accumulator
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

        // Load operand
        LoadOperandForLogical(ilGenerator, instruction, gameClass);

        // Perform XOR operation
        ilGenerator.Emit(OpCodes.Xor);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

        // Update flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleMemoryIncrement(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
        var writeMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.WriteMemory));

        if (readMemoryMethod == null || writeMemoryMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: Memory access methods not found for INC");
            return;
        }

        var addressLocal = ilGenerator.DeclareLocal(typeof(ushort));
        var valueLocal = ilGenerator.DeclareLocal(typeof(byte));

        // Calculate and store address
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, addressLocal);

        // Read current value
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldloc, addressLocal);
        ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

        // Increment and mask to byte
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Add);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Stloc, valueLocal);

        // Write back to memory
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldloc, addressLocal);
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
        ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);

        // Update flags using the new value
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
        ilGenerator.Emit(OpCodes.Dup);
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void HandleMemoryDecrement(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
        var writeMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.WriteMemory));

        if (readMemoryMethod == null || writeMemoryMethod == null)
        {
            ilGenerator.EmitWriteLine("Error: Memory access methods not found for DEC");
            return;
        }

        var addressLocal = ilGenerator.DeclareLocal(typeof(ushort));
        var valueLocal = ilGenerator.DeclareLocal(typeof(byte));

        // Calculate and store address
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Stloc, addressLocal);

        // Read current value
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldloc, addressLocal);
        ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

        // Decrement and mask to byte
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Sub);
        ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
        ilGenerator.Emit(OpCodes.And);
        ilGenerator.Emit(OpCodes.Stloc, valueLocal);

        // Write back to memory
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldloc, addressLocal);
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
        ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);

        // Update flags using the new value
        ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
        ilGenerator.Emit(OpCodes.Dup);
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private void LoadOperandForLogical(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
        }
        else
        {
            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            if (readMemoryMethod != null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
            }
            else
            {
                ilGenerator.EmitWriteLine("Error: ReadMemory method not found");
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }
    }
}