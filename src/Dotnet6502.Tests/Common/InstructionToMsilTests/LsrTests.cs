using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 LSR (Logical Shift Right) instruction
///
/// LSR shifts all bits right one position. Bit 7 is filled with 0.
/// The original bit 0 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 0 before the shift
/// - Zero: Set if result is 0
/// - Negative: Always cleared (bit 7 becomes 0)
/// </summary>
public class LsrTests
{
    [Fact]
    public void LSR_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x46);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x46, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.MemoryMap.MemoryBlock[0x10] = 0xFE;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x10].ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPage_Carry_And_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x46);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x46, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.MemoryMap.MemoryBlock[0x20] = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x20].ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x56);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x56, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x05;
        jit.MemoryMap.MemoryBlock[0x35] = 0xAA;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x35].ShouldBe((byte)0x55);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x56);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x56, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x02;
        jit.MemoryMap.MemoryBlock[0x01] = 0x63;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x01].ShouldBe((byte)0x31);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.MemoryMap.MemoryBlock[0x3000] = 0x88;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x3000].ShouldBe((byte)0x44);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x5E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x5E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x0F;
        jit.MemoryMap.MemoryBlock[0x200F] = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.MemoryMap.MemoryBlock[0x200F].ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void LSR_Accumulator_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4A);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4A],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFE;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}