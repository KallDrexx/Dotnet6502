using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles compare instructions (CMP, CPX, CPY)
/// </summary>
public class CompareHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["CMP", "CPX", "CPY"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        var sourceRegister = instruction.Info.Mnemonic switch
        {
            "CMP" => gameClass.Registers.Accumulator,
            "CPX" => gameClass.Registers.XIndex,
            "CPY" => gameClass.Registers.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };

        // Load the register value
        ilGenerator.Emit(OpCodes.Ldsfld, sourceRegister);

        // Load the comparison value
        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
        }
        else
        {
            var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
            ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);
        }

        // Perform comparison (register - value)
        ilGenerator.Emit(OpCodes.Sub);
        ilGenerator.Emit(OpCodes.Dup); // For zero flag
        ilGenerator.Emit(OpCodes.Dup); // For negative flag

        // Set carry flag if register >= value (no borrow)
        ilGenerator.Emit(OpCodes.Ldsfld, sourceRegister);
        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
        }
        else
        {
            // Reload the memory value for comparison
            var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
            ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);
        }
        ilGenerator.Emit(OpCodes.Clt); // 1 if register < value, 0 otherwise
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor); // Invert: 0 if register < value, 1 if register >= value
        IlUtils.SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Carry);

        // Update zero and negative flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }
}