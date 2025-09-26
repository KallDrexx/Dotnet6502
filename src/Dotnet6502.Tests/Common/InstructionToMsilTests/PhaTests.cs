using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
        testRunner.TestHal.ARegister = 0x42;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42); // Accumulator preserved
        testRunner.TestHal.PopFromStack().ShouldBe((byte)0x42); // Value on stack
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
        testRunner.TestHal.ARegister = 0x00;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x00);
        testRunner.TestHal.PopFromStack().ShouldBe((byte)0x00);
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
        testRunner.TestHal.ARegister = 0xFF;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0xFF);
        testRunner.TestHal.PopFromStack().ShouldBe((byte)0xFF);
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
        testRunner.TestHal.ARegister = 0x80;

        // Set all flags to test they are preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // All flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
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
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.RunTestMethod();

        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
        testRunner.TestHal.PopFromStack().ShouldBe((byte)0x42);
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
        testRunner1.TestHal.ARegister = 0x55;
        testRunner1.RunTestMethod();
        testRunner1.TestHal.PopFromStack().ShouldBe((byte)0x55);

        var testRunner2 = new InstructionTestRunner(nesIrInstructions);
        testRunner2.TestHal.ARegister = 0xAA;
        testRunner2.RunTestMethod();
        testRunner2.TestHal.PopFromStack().ShouldBe((byte)0xAA);
    }
}