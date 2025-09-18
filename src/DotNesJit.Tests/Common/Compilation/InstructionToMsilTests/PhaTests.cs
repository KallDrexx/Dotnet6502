using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PHA (Push Accumulator) instruction
///
/// PHA pushes the accumulator onto the stack and:
/// - Does NOT affect any flags
/// - Decrements the stack pointer
/// - Preserves all registers
/// </summary>
public class PhaTests
{
    [Fact]
    public void PHA_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42); // Accumulator preserved
        testRunner.NesHal.PopFromStack().ShouldBe((byte)0x42); // Value on stack
    }

    [Fact]
    public void PHA_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x00);
        testRunner.NesHal.PopFromStack().ShouldBe((byte)0x00);
    }

    [Fact]
    public void PHA_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.NesHal.PopFromStack().ShouldBe((byte)0xFF);
    }

    [Fact]
    public void PHA_Does_Not_Affect_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x80;

        // Set all flags to test they are preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // All flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void PHA_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.RunTestMethod();

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
        testRunner.NesHal.PopFromStack().ShouldBe((byte)0x42);
    }

    [Fact]
    public void PHA_Multiple_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x48);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x48],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test pushing multiple values
        var testRunner1 = new InstructionTestRunner(nesIrInstructions);
        testRunner1.NesHal.ARegister = 0x55;
        testRunner1.RunTestMethod();
        testRunner1.NesHal.PopFromStack().ShouldBe((byte)0x55);

        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.NesHal.ARegister = 0xAA;
        testRunner2.RunTestMethod();
        testRunner2.NesHal.PopFromStack().ShouldBe((byte)0xAA);
    }
}