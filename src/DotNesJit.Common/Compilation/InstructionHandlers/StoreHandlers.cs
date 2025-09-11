using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
            "STA" => gameClass.Registers.Accumulator,
            "STX" => gameClass.Registers.XIndex,
            "STY" => gameClass.Registers.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };


        // Call HAL to write to that memory location
        var setMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.WriteMemory));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Ldsfld, sourceRegister);
        ilGenerator.Emit(OpCodes.Callvirt, setMemoryMethod!);
    }
}