using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CLD (Clear Decimal Flag) instruction
///
/// CLD clears the decimal flag and:
/// - Only affects the decimal flag (sets it to 0)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class CldTests
{
    [Fact]
    public void CLD_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set decimal flag initially
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.RunMethod(0x1234);

        // Decimal flag should be cleared
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }

    [Fact]
    public void CLD_When_Already_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Decimal flag already clear
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        jit.RunMethod(0x1234);

        // Decimal flag should remain clear
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }

    [Fact]
    public void CLD_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;

        jit.RunMethod(0x1234);

        // Only decimal flag should be cleared
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();

        // All other flags should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CLD_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only decimal flag should be cleared
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
    }
}