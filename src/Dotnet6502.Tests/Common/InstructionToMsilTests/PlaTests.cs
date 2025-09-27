using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PLA (Pull Accumulator) instruction
///
/// PLA pulls a value from the stack into the accumulator and:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// - Increments the stack pointer
/// </summary>
public class PlaTests
{
    [Fact]
    public void PLA_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.PushToStack(0x42);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void PLA_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.PushToStack(0x00);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void PLA_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.PushToStack(0x80);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLA_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.PushToStack(0xFF);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PLA_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.PushToStack(0x7F);

        // Set some flags that should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void PLA_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.PushToStack(0x42);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
    }

    [Fact]
    public void PLA_Multiple_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x68);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x68],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test pulling multiple different values
        var jit1 = new TestJitCompiler();
            jit1.AddMethod(0x1234, nesIrInstructions);
        jit1.TestHal.ARegister = 0x00;
        jit1.TestHal.PushToStack(0x55);
        jit1.RunMethod(0x1234);
        jit1.TestHal.ARegister.ShouldBe((byte)0x55);
        jit1.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit1.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        var jit2 = new TestJitCompiler();
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.ARegister = 0xFF;
        jit2.TestHal.PushToStack(0xAA);
        jit2.RunMethod(0x1234);
        jit2.TestHal.ARegister.ShouldBe((byte)0xAA);
        jit2.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit2.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }
}