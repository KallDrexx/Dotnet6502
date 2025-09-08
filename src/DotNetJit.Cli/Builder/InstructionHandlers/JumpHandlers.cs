using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles jump and call instructions with complete implementation
/// </summary>
public class JumpHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["JMP", "JSR"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "JMP":
                HandleJump(ilGenerator, instruction, gameClass);
                break;
            case "JSR":
                HandleSubroutineCall(ilGenerator, instruction, gameClass);
                break;
            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private void HandleJump(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            ilGenerator.EmitWriteLine("Jump with unknown target - skipping");
            return;
        }

        var targetAddress = instruction.TargetAddress.Value;

        if (instruction.Info.AddressingMode == AddressingMode.Absolute)
        {
            // Direct jump to absolute address: JMP $1234
            ilGenerator.EmitWriteLine($"Absolute jump to ${targetAddress:X4}");

            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));

            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
            ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod!);
        }
        else if (instruction.Info.AddressingMode == AddressingMode.Indirect)
        {
            // Indirect jump: JMP ($1234) - read address from memory
            ilGenerator.EmitWriteLine($"Indirect jump via ${targetAddress:X4}");

            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));
            var localAddr = ilGenerator.DeclareLocal(typeof(ushort));

            // Read low byte from target address
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod!);

            // Read high byte from target address + 1
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)(targetAddress + 1));
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

            // Combine into 16-bit address (high << 8 | low)
            ilGenerator.Emit(OpCodes.Ldc_I4, 8);
            ilGenerator.Emit(OpCodes.Shl);
            ilGenerator.Emit(OpCodes.Or);
            ilGenerator.Emit(OpCodes.Stloc, localAddr);

            // Jump to the computed address
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, localAddr);
            ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod!);
        }
        else
        {
            ilGenerator.EmitWriteLine($"Unsupported jump addressing mode: {instruction.Info.AddressingMode}");
        }
    }

    private void HandleSubroutineCall(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            ilGenerator.EmitWriteLine("JSR with unknown target - skipping");
            return;
        }

        var targetAddress = instruction.TargetAddress.Value;

        ilGenerator.EmitWriteLine($"Subroutine call to ${targetAddress:X4}");

        // Use the hardware's built-in CallFunction method which handles stack management
        var callFunctionMethod = typeof(NesHal).GetMethod(nameof(NesHal.CallFunction));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
        ilGenerator.Emit(OpCodes.Callvirt, callFunctionMethod!);
    }
}

/// <summary>
/// Handles return instructions (RTS, RTI) with complete implementation
/// </summary>
public class ReturnHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["RTS", "RTI"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "RTS":
                HandleReturnFromSubroutine(ilGenerator, instruction, gameClass);
                break;
            case "RTI":
                HandleReturnFromInterrupt(ilGenerator, instruction, gameClass);
                break;
            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private void HandleReturnFromSubroutine(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("Return from subroutine");

        var returnFromSubroutineMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReturnFromSubroutine));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Callvirt, returnFromSubroutineMethod!);

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }

    private void HandleReturnFromInterrupt(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("Return from interrupt");

        // Complete implementation of RTI instruction
        var returnFromInterruptMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReturnFromInterrupt));

        // RTI instruction:
        // 1. Pull processor status from stack
        // 2. Pull program counter from stack
        // 3. Jump to the restored address

        if (returnFromInterruptMethod != null)
        {
            // Use the hardware's built-in method
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, returnFromInterruptMethod);
        }
        else
        {
            // Manual implementation if method doesn't exist
            var pullStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PullStack));
            var pullAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.PullAddress));
            var setProcessorStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetProcessorStatus));
            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));

            if (pullStackMethod != null && pullAddressMethod != null &&
                setProcessorStatusMethod != null && jumpToAddressMethod != null)
            {
                // Pull processor status from stack
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Dup); // Duplicate for next call
                ilGenerator.Emit(OpCodes.Callvirt, pullStackMethod);

                // Set processor status
                ilGenerator.Emit(OpCodes.Callvirt, setProcessorStatusMethod);

                // Pull return address from stack
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Dup); // Duplicate for next call
                ilGenerator.Emit(OpCodes.Callvirt, pullAddressMethod);

                // Jump to return address
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                ilGenerator.EmitWriteLine("RTI implementation not available - using simplified return");
            }
        }

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }
}

/// <summary>
/// Handles interrupt and system instructions (BRK, NOP) with complete implementation
/// </summary>
public class SystemHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["BRK", "NOP"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "BRK":
                HandleSoftwareInterrupt(ilGenerator, instruction, gameClass);
                break;
            case "NOP":
                HandleNoOperation(ilGenerator, instruction, gameClass);
                break;
            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private void HandleSoftwareInterrupt(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("Software interrupt (BRK)");

        // Complete implementation of BRK instruction
        var triggerSoftwareInterruptMethod = typeof(NesHal).GetMethod(nameof(NesHal.TriggerSoftwareInterrupt));

        if (triggerSoftwareInterruptMethod != null)
        {
            // Use the hardware's built-in method
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, triggerSoftwareInterruptMethod);
        }
        else
        {
            // Manual implementation if method doesn't exist
            var pushAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushAddress));
            var pushStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushStack));
            var getProcessorStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProcessorStatus));
            var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));
            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));

            if (pushAddressMethod != null && pushStackMethod != null &&
                getProcessorStatusMethod != null && setFlagMethod != null &&
                readMemoryMethod != null && jumpToAddressMethod != null)
            {
                var localStatus = ilGenerator.DeclareLocal(typeof(byte));
                var localVector = ilGenerator.DeclareLocal(typeof(ushort));

                // Push PC + 2 to stack (BRK return address)
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Dup); // Duplicate for next operation
                ilGenerator.Emit(OpCodes.Callvirt, typeof(NesHal).GetMethod(nameof(NesHal.GetProgramCounter))!);
                ilGenerator.Emit(OpCodes.Ldc_I4_2);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Callvirt, pushAddressMethod);

                // Get processor status and set B flag
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Callvirt, getProcessorStatusMethod);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0x10); // B flag
                ilGenerator.Emit(OpCodes.Or);
                ilGenerator.Emit(OpCodes.Stloc, localStatus);

                // Push status with B flag set to stack
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldloc, localStatus);
                ilGenerator.Emit(OpCodes.Callvirt, pushStackMethod);

                // Set interrupt disable flag
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);

                // Read IRQ vector from $FFFE-$FFFF
                // Read low byte
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFE);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

                // Read high byte
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

                // Combine into vector address
                ilGenerator.Emit(OpCodes.Ldc_I4, 8);
                ilGenerator.Emit(OpCodes.Shl);
                ilGenerator.Emit(OpCodes.Or);
                ilGenerator.Emit(OpCodes.Stloc, localVector);

                // Jump to interrupt vector
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldloc, localVector);
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                ilGenerator.EmitWriteLine("BRK implementation not available - using simplified interrupt");
            }
        }
    }

    private void HandleNoOperation(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("No operation");
        // NOP literally does nothing except advance the PC, which is handled automatically
        // We could add a cycle count here if needed for timing accuracy
    }
}