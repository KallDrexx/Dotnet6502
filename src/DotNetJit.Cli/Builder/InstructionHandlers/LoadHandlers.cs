using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles load related instructions
/// </summary>
public class LoadHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["LDA", "LDX"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var targetRegister = instruction.Info.Mnemonic switch
        {
            "LDA" => gameClass.CpuRegisters.Accumulator,
            "LDX" => gameClass.CpuRegisters.XIndex,
            "LDY" => gameClass.CpuRegisters.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };

        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            // Saves a constant straight to the accumulator
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
            ilGenerator.Emit(OpCodes.Stsfld, targetRegister);
            return;
        }

        // Call the read memory handler and save the value in the target register
        var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);
        ilGenerator.Emit(OpCodes.Stsfld, targetRegister);
    }
}