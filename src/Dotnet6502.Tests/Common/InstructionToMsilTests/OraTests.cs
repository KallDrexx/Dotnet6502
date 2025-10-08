using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ORA (Logical OR) instruction
///
/// ORA performs a bitwise OR between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class OraTests
{
    [Fact]
    public void ORA_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x10;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x1F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }

    [Fact]
    public void ORA_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x81);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void ORA_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x05);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x05, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x55;
        jit.MemoryMap.MemoryBlock[0x10] = 0xAA;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ORA_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x30;
        jit.TestHal.XRegister = 0x05;
        jit.MemoryMap.MemoryBlock[0x35] = 0x0C;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x3C);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ORA_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x15);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x15, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.XRegister = 0x02;
        jit.MemoryMap.MemoryBlock[0x01] = 0x7E;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ORA_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x0D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x0D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x41;
        jit.MemoryMap.MemoryBlock[0x3000] = 0x82;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xC3);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ORA_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x0F;
        jit.MemoryMap.MemoryBlock[0x200F] = 0x24;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x66);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void ORA_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x19);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x19, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x0F;
        jit.TestHal.YRegister = 0x01;
        jit.MemoryMap.MemoryBlock[0x5000] = 0xE0;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xEF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ORA_Pattern_Combine()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0xCC],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void ORA_Load_Pattern()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x09);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x09, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}