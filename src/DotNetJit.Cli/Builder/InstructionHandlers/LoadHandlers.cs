using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles load related instructions - FIXED VERSION
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
            var immediateValue = (int)instruction.Operands[0];

            ilGenerator.Emit(OpCodes.Ldc_I4, immediateValue);
            ilGenerator.Emit(OpCodes.Stsfld, targetRegister);

            // Update flags for the loaded value - fix the stack underflow
            ilGenerator.Emit(OpCodes.Ldc_I4, immediateValue); // Push value for zero flag
            ilGenerator.Emit(OpCodes.Dup); // Duplicate for negative flag
            IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
            IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
            return;
        }

        // For memory addressing modes, call the memory read handler
        var getMemoryValueMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
        if (getMemoryValueMethod == null)
        {
            ilGenerator.EmitWriteLine($"Error: ReadMemory method not found for {instruction.Info.Mnemonic}");
            return;
        }

        // Load hardware instance and address onto stack
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod);

        // Store the result and prepare for flag updates
        ilGenerator.Emit(OpCodes.Dup); // Duplicate for storing
        ilGenerator.Emit(OpCodes.Dup); // Duplicate for zero flag  
        ilGenerator.Emit(OpCodes.Dup); // Duplicate for negative flag
        ilGenerator.Emit(OpCodes.Stsfld, targetRegister);

        // Update zero and negative flags
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }
}