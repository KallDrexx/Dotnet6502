using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LDX (Load X Register) instruction
///
/// LDX loads a value from memory or immediate into the X register and affects:
/// - Zero flag: Set if result is 0
/// - Negative flag: Set if bit 7 is set
/// - Does NOT affect Carry or Overflow flags
/// </summary>
public class LdxTests
{
    [Fact]
    public void LDX_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Immediate_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Immediate_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x80);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Immediate_HighValue()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA6, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.MemoryValues[0x10] = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_ZeroPage_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA6, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.TestHal.MemoryValues[0x20] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_ZeroPageY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB6, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x05;
        jit.TestHal.MemoryValues[0x35] = 0x55; // 0x30 + 0x05
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x55);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_ZeroPageY_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB6, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x02;
        jit.TestHal.MemoryValues[0x01] = 0x77; // (0xFF + 0x02) & 0xFF = 0x01
        jit.TestHal.MemoryValues[0x101] = 0x88; // Should NOT be accessed
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x77);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAE, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.MemoryValues[0x3000] = 0x99;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x99);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBE, 0x00, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x10;
        jit.TestHal.MemoryValues[0x4010] = 0x22;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x22);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_AbsoluteY_PageCrossing()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xBE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xBE, 0xFF, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x02;
        jit.TestHal.MemoryValues[0x2101] = 0x88; // 0x20FF + 0x02 = 0x2101
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x88);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;

        // Set some flags that should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;

        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x7F);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();

        // These flags should be preserved
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
    }

    [Fact]
    public void LDX_Does_Not_Affect_A_Or_Y_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xA2);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xA2, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x11;
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x22;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.ARegister.ShouldBe((byte)0x11); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x22); // Should remain unchanged
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_ZeroPageY_Negative_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB6);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB6, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.TestHal.YRegister = 0x08;
        jit.TestHal.MemoryValues[0x48] = 0xC0; // 0x40 + 0x08, negative value
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xC0);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void LDX_Absolute_Zero_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xAE);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xAE, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.TestHal.MemoryValues[0x1234] = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}