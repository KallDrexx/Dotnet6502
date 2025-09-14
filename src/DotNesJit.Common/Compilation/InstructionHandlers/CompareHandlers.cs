using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.InstructionHandlers;

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
            var getMemoryValueMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReadMemory));
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
            IlUtils.LoadAddressToStack(instruction, gameClass, ilGenerator);
            ilGenerator.Emit(OpCodes.Callvirt, getMemoryValueMethod!);
        }

        // Perform comparison (register - value)
        ilGenerator.Emit(OpCodes.Sub);

        // Carry flag if register >= memory (subtraction >= 0)
        IlUtils.AddMsilComment(ilGenerator, "Compare: set carry flag");
        // ilGenerator.Emit(OpCodes.Dup);
        CompareGreaterThanOrEqualToZero(ilGenerator);
        IlUtils.SetFlagFromIlStack(gameClass, ilGenerator, CpuStatusFlags.Carry);

        return;

        var sourceRegisterLocal = ilGenerator.DeclareLocal(typeof(byte));
        var memoryLocal = ilGenerator.DeclareLocal(typeof(byte));

        ilGenerator.Emit(OpCodes.Stloc, memoryLocal);
        ilGenerator.Emit(OpCodes.Stloc, sourceRegisterLocal);

        // Set carry flag if register >= value (no borrow)
        ilGenerator.Emit(OpCodes.Ldloc, sourceRegisterLocal);
        ilGenerator.Emit(OpCodes.Ldloc, memoryLocal);
        CompareGreaterThanOrEqualToZero(ilGenerator);


        // Zero flag
        ilGenerator.Emit(OpCodes.Ldloc, sourceRegisterLocal);
        ilGenerator.Emit(OpCodes.Ldloc, memoryLocal);
        ilGenerator.Emit(OpCodes.Ceq);
        IlUtils.UpdateZeroFlag(gameClass, ilGenerator);

        // Negative flag
        ilGenerator.Emit(OpCodes.Ldloc, sourceRegisterLocal);
        ilGenerator.Emit(OpCodes.Ldloc, memoryLocal);
        ilGenerator.Emit(OpCodes.Sub);
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Clt);
        IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
    }

    private static void CompareGreaterThanOrEqualToZero(ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
        ilGenerator.Emit(OpCodes.Clt); // 1 if value on the stack < 0, 0 otherwise
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Xor); // Invert: 0 if register < value, 1 if register >= value
    }
}