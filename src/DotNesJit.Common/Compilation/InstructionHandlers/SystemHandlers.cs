using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
        IlUtils.AddMsilComment(ilGenerator, "Software interrupt (BRK)");

        // SIMPLIFIED: Use hardware method instead of complex interrupt implementation
        var triggerSoftwareInterruptMethod = typeof(INesHal).GetMethod(nameof(INesHal.TriggerSoftwareInterrupt));
        if (triggerSoftwareInterruptMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, triggerSoftwareInterruptMethod);
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, "// Would trigger software interrupt (method not found)");
        }
    }

    private void HandleNoOperation(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        IlUtils.AddMsilComment(ilGenerator, "No operation");
        // NOP literally does nothing except advance the PC, which is handled automatically
        // We could add a cycle count here if needed for timing accuracy
    }
}