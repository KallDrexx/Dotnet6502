using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ROR (Rotate Right through Carry) instruction
///
/// ROR shifts all bits right one position. The Carry flag is shifted into bit 7.
/// The original bit 0 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 0 before the rotation
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set (which comes from the old Carry flag)
/// </summary>
public class RorTests
{
    // Note: ROR Accumulator mode tests are not included as the InstructionConverter
    // does not yet support the Accumulator addressing mode for shift instructions

    [Fact]
    public void ROR_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x66);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x66, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.MemoryValues[0x10] = 0xAA;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x10].ShouldBe((byte)0x55);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROR_ZeroPage_Carry_In()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x66);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x66, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.MemoryValues[0x20] = 0x7E;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x20].ShouldBe((byte)0xBF);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROR_ZeroPageX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x76);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x76, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x05;
        jit.TestHal.MemoryValues[0x35] = 0x83;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x35].ShouldBe((byte)0x41);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROR_ZeroPageX_Wraparound()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x76);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x76, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x02;
        jit.TestHal.MemoryValues[0x01] = 0x66;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x01].ShouldBe((byte)0xB3);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROR_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.MemoryValues[0x3000] = 0x84;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x3000].ShouldBe((byte)0x42);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }

    [Fact]
    public void ROR_AbsoluteX_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x7E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x7E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x0F;
        jit.TestHal.MemoryValues[0x200F] = 0x02;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x200F].ShouldBe((byte)0x81);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void ROR_AbsoluteX_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x7E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x7E, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x01;
        jit.TestHal.MemoryValues[0x5000] = 0x01;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = false;
        jit.RunMethod(0x1234);

        jit.TestHal.MemoryValues[0x5000].ShouldBe((byte)0x00);
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
    }
}