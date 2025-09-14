using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
            IlUtils.AddMsilComment(ilGenerator, "Jump with unknown target - skipping");
            return;
        }

        var targetAddress = instruction.TargetAddress.Value;

        if (instruction.Info.AddressingMode == AddressingMode.Absolute)
        {
            // Direct jump to absolute address: JMP $1234
            IlUtils.AddMsilComment(ilGenerator, $"Absolute jump to ${targetAddress:X4}");

            // SIMPLIFIED: Just call the hardware jump method instead of complex IL generation
            var jumpToAddressMethod = typeof(INesHal).GetMethod(nameof(INesHal.JumpToAddress));
            if (jumpToAddressMethod != null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                IlUtils.AddMsilComment(ilGenerator, $"// Would jump to ${targetAddress:X4} (method not found)");
            }
        }
        else if (instruction.Info.AddressingMode == AddressingMode.Indirect)
        {
            // Indirect jump: JMP ($1234) - read address from memory
            IlUtils.AddMsilComment(ilGenerator, $"Indirect jump via ${targetAddress:X4}");

            // SIMPLIFIED: Use hardware methods instead of complex IL generation
            var readMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReadMemory));
            var jumpToAddressMethod = typeof(INesHal).GetMethod(nameof(INesHal.JumpToAddress));

            if (readMemoryMethod != null && jumpToAddressMethod != null)
            {
                var localAddr = ilGenerator.DeclareLocal(typeof(ushort));

                // Read low byte from target address
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

                // Read high byte from target address + 1
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)(targetAddress + 1));
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

                // Combine into 16-bit address (high << 8 | low)
                ilGenerator.Emit(OpCodes.Ldc_I4, 8);
                ilGenerator.Emit(OpCodes.Shl);
                ilGenerator.Emit(OpCodes.Or);
                ilGenerator.Emit(OpCodes.Stloc, localAddr);

                // Jump to the computed address
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldloc, localAddr);
                ilGenerator.Emit(OpCodes.Callvirt, jumpToAddressMethod);
            }
            else
            {
                IlUtils.AddMsilComment(ilGenerator, $"// Would perform indirect jump via ${targetAddress:X4} (methods not found)");
            }
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, $"Unsupported jump addressing mode: {instruction.Info.AddressingMode}");
        }
    }

    private void HandleSubroutineCall(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            IlUtils.AddMsilComment(ilGenerator, "JSR with unknown target - skipping");
            return;
        }

        var targetAddress = instruction.TargetAddress.Value;

        IlUtils.AddMsilComment(ilGenerator, $"Subroutine call to ${targetAddress:X4}");

        // SIMPLIFIED: Use the hardware's built-in CallFunction method
        var callFunctionMethod = typeof(INesHal).GetMethod(nameof(INesHal.CallFunction));
        if (callFunctionMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
            ilGenerator.Emit(OpCodes.Callvirt, callFunctionMethod);
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, $"// Would call subroutine at ${targetAddress:X4} (method not found)");
        }
    }
}