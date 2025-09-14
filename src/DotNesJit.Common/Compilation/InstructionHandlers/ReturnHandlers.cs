using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
        IlUtils.AddMsilComment(ilGenerator, "Return from subroutine");

        // SIMPLIFIED: Use hardware method instead of complex stack manipulation
        var returnFromSubroutineMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReturnFromSubroutine));
        if (returnFromSubroutineMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Callvirt, returnFromSubroutineMethod);
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, "// Would return from subroutine (method not found)");
        }

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }

    private void HandleReturnFromInterrupt(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        IlUtils.AddMsilComment(ilGenerator, "Return from interrupt");

        // SIMPLIFIED: Use hardware method instead of complex interrupt handling
        var returnFromInterruptMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReturnFromInterrupt));
        if (returnFromInterruptMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Callvirt, returnFromInterruptMethod);
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, "// Would return from interrupt (method not found)");
        }

        // Return from this method as well
        ilGenerator.Emit(OpCodes.Ret);
    }
}