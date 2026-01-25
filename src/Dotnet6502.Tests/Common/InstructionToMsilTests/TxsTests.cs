using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 TXS (Transfer X to Stack Pointer) instruction
///
/// TXS transfers the value in the X register to the stack pointer and:
/// - Does NOT affect any flags (unique among transfer instructions)
/// - Does NOT affect the X register (preserves it)
/// - Does NOT affect other registers
/// </summary>
public class TxsTests
{
    [Fact]
    public void TXS_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x42;
        jit.TestHal.StackPointer = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x42); // X register preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void TXS_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.StackPointer = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x00);
        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        // TXS does NOT set zero flag even when transferring zero
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void TXS_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x80;
        jit.TestHal.StackPointer = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x80);
        jit.TestHal.XRegister.ShouldBe((byte)0x80);
        // TXS does NOT set negative flag even when transferring negative value
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void TXS_High_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.TestHal.StackPointer = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);
        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void TXS_Preserves_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x80; // Negative value
        jit.TestHal.StackPointer = 0xFF;

        // Set all flags to test they are preserved
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x80);
        jit.TestHal.XRegister.ShouldBe((byte)0x80);

        // All flags should be preserved (TXS is unique in not affecting flags)
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void TXS_Does_Not_Affect_Other_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x42;
        jit.TestHal.YRegister = 0x33;
        jit.TestHal.StackPointer = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x33); // Should remain unchanged
    }

    [Fact]
    public void TXS_Preserves_Clear_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x9A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x9A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00; // Zero value
        jit.TestHal.StackPointer = 0xFF;

        // Clear all flags to test they remain clear
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        jit.RunMethod(0x1234);

        jit.TestHal.StackPointer.ShouldBe((byte)0x00);
        jit.TestHal.XRegister.ShouldBe((byte)0x00);

        // All flags should remain clear
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}