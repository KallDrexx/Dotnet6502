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
    public override string[] Mnemonics => ["LDA", "LDX", "LDY"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var targetRegister = instruction.Info.Mnemonic switch
        {
            "LDA" => gameClass.Registers.Accumulator,
            "LDX" => gameClass.Registers.XIndex,
            "LDY" => gameClass.Registers.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };

        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            // Load a constant directly into the register
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
            ilGenerator.Emit(OpCodes.Stsfld, targetRegister);

            // Update flags for the loaded value
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
            IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
            IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
            return;
        }

        // For memory addressing modes, call the memory read handler
        var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);

        // Store the value and update flags
        ilGenerator.Emit(OpCodes.Dup); // Duplicate for flag updates
        ilGenerator.Emit(OpCodes.Dup); // Duplicate again for both flags
        ilGenerator.Emit(OpCodes.Stsfld, targetRegister);

        // Update zero and negative flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }
}