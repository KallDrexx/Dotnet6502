using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 EOR (Exclusive OR) instruction
///
/// EOR performs a bitwise exclusive OR between the accumulator and a memory value or immediate value.
/// The result is stored in the accumulator.
///
/// Flags affected:
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// - Carry and Overflow flags are NOT affected
/// </summary>
public class EorTests
{
    [Fact]
    public void EOR_Immediate_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x0F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x33;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x3C);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x7F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7E);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Immediate_Flip_Sign()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x81);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x45);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x45, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x55;
        jit.Memory.MemoryBlock[0x10] = 0xAA;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x55);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x55, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x33;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x33;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void EOR_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x55);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x55, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x81;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x01;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4D, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xC3;
        jit.Memory.MemoryBlock[0x3000] = 0x3C;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x5D);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x5D, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x66;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x24);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void EOR_AbsoluteY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x59);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x59, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x0F;
        jit.TestHal.YRegister = 0x01;
        jit.Memory.MemoryBlock[0x5000] = 0xF0;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Pattern_Toggle()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x55],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xAA;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void EOR_Bit_Test()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x49);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x49, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}