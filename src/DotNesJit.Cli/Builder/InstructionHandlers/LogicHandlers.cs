using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles logic based opcodes
/// </summary>
public class LogicHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["BIT"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));

        switch (instruction.Info.Mnemonic)
        {
            case "BIT":
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!); // Load the value in memory
                ilGenerator.Emit(OpCodes.And); // for negative flag
                ilGenerator.Emit(OpCodes.Dup); // for overflow flag
                ilGenerator.Emit(OpCodes.Dup); // for zero flag

                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateOverflowFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}