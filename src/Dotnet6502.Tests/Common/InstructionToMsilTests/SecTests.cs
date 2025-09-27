using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SEC (Set Carry Flag) instruction
///
/// SEC sets the carry flag and:
/// - Only affects the carry flag (sets it to 1)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class SecTests
{
    [Fact]
    public void SEC_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear carry flag initially
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.RunMethod(0x1234);

        // Carry flag should be set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_When_Already_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Carry flag already set
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        // Carry flag should remain set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags except carry
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;

        jit.RunMethod(0x1234);

        // Only carry flag should be set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void SEC_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
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
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only carry flag should be set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void SEC_With_Mixed_Initial_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set a mixed pattern of flags
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false; // Will be set
        jit.TestHal.Flags[CpuStatusFlags.Zero] = false; // Should remain false
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;  // Should remain true
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = false; // Should remain false
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;  // Should remain true
        jit.TestHal.Flags[CpuStatusFlags.Negative] = false; // Should remain false

        jit.RunMethod(0x1234);

        // Only carry flag should be affected
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain unchanged
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SEC_Preserves_Clear_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.TestHal.Flags[CpuStatusFlags.Zero] = false;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = false;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = false;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = false;

        jit.RunMethod(0x1234);

        // Only carry flag should be set
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // All other flags should remain clear
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void SEC_Multiple_Calls()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear carry flag initially
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;

        // First SEC call
        jit.RunMethod(0x1234);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();

        // Second SEC call (should have no effect)
        var jit2 = new TestJitCompiler();
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.Flags[CpuStatusFlags.Carry] = true; // Already set
        jit2.RunMethod(0x1234);
        jit2.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }
}