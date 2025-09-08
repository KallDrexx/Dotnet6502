using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles the core store opcodes
/// </summary>
public class StoreHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["STA", "STX", "STY"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var sourceRegister = instruction.Info.Mnemonic switch
        {
            "STA" => gameClass.CpuRegisters.Accumulator,
            "STX" => gameClass.CpuRegisters.XIndex,
            "STY" => gameClass.CpuRegisters.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };


        // Call HAL to write to that memory location
        var setMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.WriteMemory));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Ldsfld, sourceRegister);
        ilGenerator.Emit(OpCodes.Callvirt, setMemoryMethod!);
    }
}