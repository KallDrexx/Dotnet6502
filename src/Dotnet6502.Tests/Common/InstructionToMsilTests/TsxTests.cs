using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 TSX (Transfer Stack Pointer to X) instruction
///
/// TSX transfers the value in the stack pointer to the X register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// - Does NOT affect the stack pointer (preserves it)
/// </summary>
public class TsxTests
{
    [Fact]
    public void TSX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0x42;
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.StackPointer.ShouldBe((byte)0x42); // Stack pointer preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void TSX_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0x00;
        jit.TestHal.XRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        jit.TestHal.StackPointer.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void TSX_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0x80;
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x80);
        jit.TestHal.StackPointer.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void TSX_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void TSX_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0x7F;
        jit.TestHal.XRegister = 0x00;

        // Set some flags that should be preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x7F);
        jit.TestHal.StackPointer.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();

        // These flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
    }

    [Fact]
    public void TSX_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.StackPointer = 0x42;
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.StackPointer.ShouldBe((byte)0x42);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void TSX_Flag_Changes_Override_Previous()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBA);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBA],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.StackPointer = 0x7F; // Positive value
        jit.TestHal.XRegister = 0x00;

        // Set opposite flags that should be overridden
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x7F);
        jit.TestHal.StackPointer.ShouldBe((byte)0x7F);
        // Flags should be updated based on the transferred value
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse(); // Overridden
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse(); // Overridden
    }
}