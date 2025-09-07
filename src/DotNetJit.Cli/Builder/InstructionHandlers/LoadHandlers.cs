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

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, NesAssemblyBuilder builder)
    {
        var targetRegister = instruction.Info.Mnemonic switch
        {
            "LDA" => builder.Hardware.Accumulator,
            "LDX" => builder.Hardware.XIndex,
            "LDY" => builder.Hardware.YIndex,
            _ => throw new NotSupportedException(instruction.Info.Mnemonic),
        };

        if (instruction.Info.AddressingMode == AddressingMode.Immediate)
        {
            LoadFromConstant(ilGenerator, targetRegister, instruction.Operands[0]);
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
                ilGenerator.Emit(OpCodes.Ldfld, builder.Hardware.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.ZeroPageY:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldfld, builder.Hardware.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.Absolute:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                break;

            case AddressingMode.AbsoluteX:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldfld, builder.Hardware.XIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            case AddressingMode.AbsoluteY:
                tempAddress = (instruction.Operands[1] << 8) | (instruction.Operands[0]);
                ilGenerator.Emit(OpCodes.Ldc_I4, tempAddress);
                ilGenerator.Emit(OpCodes.Ldfld, builder.Hardware.YIndex);
                ilGenerator.Emit(OpCodes.Add);
                break;

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }

        // Store the final value on the stack into a local
        var sourceAddress = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, sourceAddress);

        // Load the memory block and index grab the value from it
        ilGenerator.Emit(OpCodes.Ldsfld, builder.Hardware.Memory);
        ilGenerator.Emit(OpCodes.Ldloc, sourceAddress);
        ilGenerator.Emit(OpCodes.Ldelem_U1);
        ilGenerator.Emit(OpCodes.Stsfld, targetRegister);
    }

    private static void LoadFromConstant(ILGenerator ilGenerator, FieldInfo target, byte value)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)value);
        ilGenerator.Emit(OpCodes.Stsfld, target);
    }
}