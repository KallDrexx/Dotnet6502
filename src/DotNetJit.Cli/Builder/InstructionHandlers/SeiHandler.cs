using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

public class SeiHandler : InstructionHandler
{
    public override string[] Mnemonics => ["SEI"];

    protected override void HandleInternal(
        ILGenerator ilGenerator,
        DisassembledInstruction instruction,
        NesAssemblyBuilder builder)
    {
        // Set the interrupt disable flag to true
        var field = builder.Hardware.InterruptDisableFlag;

        ilGenerator.Emit(OpCodes.Ldc_I4, 1);
        ilGenerator.Emit(OpCodes.Stsfld, field);
    }
}