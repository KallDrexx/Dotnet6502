using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

public class SeiHandler : InstructionHandler
{
    public override string[] Mnemonics => ["SEI"];

    protected override void HandleInternal(
        ILGenerator ilGenerator,
        DisassembledInstruction instruction,
        GameClass gameClass)
    {
        var setFlagMethod = typeof(NesHardware).GetMethod(nameof(NesHardware.SetFlag));

        // Set the interrupt disable flag to true
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
        ilGenerator.Emit(OpCodes.Ldc_I4, 1);
        ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod!);
    }
}