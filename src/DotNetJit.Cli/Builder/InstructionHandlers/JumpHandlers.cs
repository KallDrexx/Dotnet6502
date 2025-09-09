using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles jump and call instructions - SIMPLIFIED VERSION
/// FIXES: Removed complex label management that was causing "Label X has not been marked" errors
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

            // SIMPLIFIED: Just call the hardware jump method instead of complex IL generation
            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));
            if (jumpToAddressMethod != null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                ilGenerator.EmitWriteLine($"// Would jump to ${targetAddress:X4} (method not found)");
            }
        }
        else if (instruction.Info.AddressingMode == AddressingMode.Indirect)
        {
            // Indirect jump: JMP ($1234) - read address from memory
            ilGenerator.EmitWriteLine($"Indirect jump via ${targetAddress:X4}");

            // SIMPLIFIED: Use hardware methods instead of complex IL generation
            var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            var jumpToAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.JumpToAddress));

            if (readMemoryMethod != null && jumpToAddressMethod != null)
            {
                var localAddr = ilGenerator.DeclareLocal(typeof(ushort));

                // Read low byte from target address
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

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
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                ilGenerator.EmitWriteLine($"// Would perform indirect jump via ${targetAddress:X4} (methods not found)");
            }
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

        // SIMPLIFIED: Use the hardware's built-in CallFunction method
        var callFunctionMethod = typeof(NesHal).GetMethod(nameof(NesHal.CallFunction));
        if (callFunctionMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
            ilGenerator.Emit(OpCodes.Callvirt, callFunctionMethod);
        }
        else
        {
            ilGenerator.EmitWriteLine($"// Would call subroutine at ${targetAddress:X4} (method not found)");
        }
    }
}

/// <summary>
/// Handles return instructions (RTS, RTI) - SIMPLIFIED VERSION
/// FIXES: Removed complex IL generation that could cause label issues
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

        // SIMPLIFIED: Use hardware method instead of complex stack manipulation
        var returnFromSubroutineMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReturnFromSubroutine));
        if (returnFromSubroutineMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, returnFromSubroutineMethod);
        }
        else
        {
            ilGenerator.EmitWriteLine("// Would return from subroutine (method not found)");
        }

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }

    private void HandleReturnFromInterrupt(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("Return from interrupt");

        // SIMPLIFIED: Use hardware method instead of complex interrupt handling
        var returnFromInterruptMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReturnFromInterrupt));
        if (returnFromInterruptMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, returnFromInterruptMethod);
        }
        else
        {
            ilGenerator.EmitWriteLine("// Would return from interrupt (method not found)");
        }

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }
}

/// <summary>
/// Handles interrupt and system instructions (BRK, NOP) - SIMPLIFIED VERSION
/// FIXES: Removed complex IL generation that could cause label issues
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

        // SIMPLIFIED: Use hardware method instead of complex interrupt implementation
        var triggerSoftwareInterruptMethod = typeof(NesHal).GetMethod(nameof(NesHal.TriggerSoftwareInterrupt));
        if (triggerSoftwareInterruptMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, triggerSoftwareInterruptMethod);
        }
        else
        {
            ilGenerator.EmitWriteLine("// Would trigger software interrupt (method not found)");
        }
    }

    private void HandleNoOperation(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine("No operation");
        // NOP literally does nothing except advance the PC, which is handled automatically
        // We could add a cycle count here if needed for timing accuracy
    }
}