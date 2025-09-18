using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PHP (Push Processor Status) instruction
///
/// PHP pushes the processor status register (flags) onto the stack and:
/// - Does NOT affect any flags
/// - Decrements the stack pointer
/// - Preserves all registers
/// - Sets the Break flag in the pushed value (real 6502 behavior)
/// </summary>
public class PhpTests
{
    [Fact]
    public void PHP_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x08);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x08],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set some flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        // Flags should be preserved
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // Check the value on the stack includes our flags
        var stackValue = testRunner.NesHal.PopFromStack();
        // Carry = bit 0, InterruptDisable = bit 2, Overflow = bit 6
        // Expected: 0x01 (Carry) | 0x04 (InterruptDisable) | 0x40 (Overflow) = 0x45
        // Plus Break flag and Always1 flag that should be set in pushed value
        (stackValue & 0x01).ShouldBe((byte)0x01); // Carry set
        (stackValue & 0x02).ShouldBe((byte)0x00); // Zero clear
        (stackValue & 0x04).ShouldBe((byte)0x04); // InterruptDisable set
        (stackValue & 0x08).ShouldBe((byte)0x00); // Decimal clear
        (stackValue & 0x40).ShouldBe((byte)0x40); // Overflow set
        (stackValue & 0x80).ShouldBe((byte)0x00); // Negative clear
    }

    [Fact]
    public void PHP_All_Flags_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x08);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x08],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // All flags should still be set
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();

        // Check all flags are reflected on stack
        var stackValue = testRunner.NesHal.PopFromStack();
        (stackValue & 0x01).ShouldBe((byte)0x01); // Carry
        (stackValue & 0x02).ShouldBe((byte)0x02); // Zero
        (stackValue & 0x04).ShouldBe((byte)0x04); // InterruptDisable
        (stackValue & 0x08).ShouldBe((byte)0x08); // Decimal
        (stackValue & 0x40).ShouldBe((byte)0x40); // Overflow
        (stackValue & 0x80).ShouldBe((byte)0x80); // Negative
    }

    [Fact]
    public void PHP_All_Flags_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x08);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x08],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Clear all flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = false;

        testRunner.RunTestMethod();

        // All flags should still be clear
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // Check all flags are clear on stack
        var stackValue = testRunner.NesHal.PopFromStack();
        (stackValue & 0x01).ShouldBe((byte)0x00); // Carry
        (stackValue & 0x02).ShouldBe((byte)0x00); // Zero
        (stackValue & 0x04).ShouldBe((byte)0x00); // InterruptDisable
        (stackValue & 0x08).ShouldBe((byte)0x00); // Decimal
        (stackValue & 0x40).ShouldBe((byte)0x00); // Overflow
        (stackValue & 0x80).ShouldBe((byte)0x00); // Negative
    }

    [Fact]
    public void PHP_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x08);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x08],
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

        testRunner.NesHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
    }

    [Fact]
    public void PHP_Specific_Flag_Pattern()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x08);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x08],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set a specific pattern: Only carry and negative flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Check that the specific pattern is pushed to stack
        var stackValue = testRunner.NesHal.PopFromStack();
        (stackValue & 0x01).ShouldBe((byte)0x01); // Carry set
        (stackValue & 0x02).ShouldBe((byte)0x00); // Zero clear
        (stackValue & 0x04).ShouldBe((byte)0x00); // InterruptDisable clear
        (stackValue & 0x08).ShouldBe((byte)0x00); // Decimal clear
        (stackValue & 0x40).ShouldBe((byte)0x00); // Overflow clear
        (stackValue & 0x80).ShouldBe((byte)0x80); // Negative set
    }
}