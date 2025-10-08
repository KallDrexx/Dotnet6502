using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set some flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        jit.RunMethod(0x1234);

        // Flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();

        // Check the value on the stack includes our flags
        var stackValue = jit.TestHal.PopFromStack();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // All flags should still be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();

        // Check all flags are reflected on stack
        var stackValue = jit.TestHal.PopFromStack();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        jit.RunMethod(0x1234);

        // All flags should still be clear
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();

        // Check all flags are clear on stack
        var stackValue = jit.TestHal.PopFromStack();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set a specific pattern: Only carry and negative flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // Check that the specific pattern is pushed to stack
        var stackValue = jit.TestHal.PopFromStack();
        (stackValue & 0x01).ShouldBe((byte)0x01); // Carry set
        (stackValue & 0x02).ShouldBe((byte)0x00); // Zero clear
        (stackValue & 0x04).ShouldBe((byte)0x00); // InterruptDisable clear
        (stackValue & 0x08).ShouldBe((byte)0x00); // Decimal clear
        (stackValue & 0x40).ShouldBe((byte)0x00); // Overflow clear
        (stackValue & 0x80).ShouldBe((byte)0x80); // Negative set
    }
}