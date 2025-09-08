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

        // Figure out the source address we want to read from
        int tempAddress;
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.ZeroPage:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                break;

            case AddressingMode.ZeroPageX:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldfld, gameClass.CpuRegisters.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.ZeroPageY:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldfld, gameClass.CpuRegisters.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.Absolute:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                break;

            case AddressingMode.AbsoluteX:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldfld, gameClass.CpuRegisters.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.AbsoluteY:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldfld, gameClass.CpuRegisters.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }

        // Store the final value on the stack into a local
        var sourceAddress = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, sourceAddress);

        // Call the read memory handler and save the value in the target register
        var getMemoryValueMethod = typeof(NesHardware).GetMethod(nameof(NesHardware.ReadMemory));

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldloc, sourceAddress);
        ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);
        ilGenerator.Emit(OpCodes.Stsfld, targetRegister);
    }
}