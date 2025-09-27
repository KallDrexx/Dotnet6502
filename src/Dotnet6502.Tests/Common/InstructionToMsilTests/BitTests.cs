using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 BIT (Bit Test) instruction
///
/// BIT performs a bitwise AND between the accumulator and a memory value,
/// but does NOT store the result. It only affects the flags.
///
/// Flags affected:
/// - Zero: Set if (A & memory) is 0
/// - Negative: Set to bit 7 of the memory value (NOT the result)
/// - Overflow: Set to bit 6 of the memory value (NOT the result)
/// - Carry flag is NOT affected
///
/// Note: BIT only supports ZeroPage and Absolute addressing modes
/// </summary>
public class BitTests
{
    [Fact]
    public void BIT_ZeroPage_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.MemoryValues[0x10] = 0x33;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void BIT_ZeroPage_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x0F;
        jit.TestHal.MemoryValues[0x20] = 0xF0;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x0F);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void BIT_ZeroPage_Negative_Flag()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.MemoryValues[0x30] = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void BIT_ZeroPage_Overflow_Flag()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x40],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.MemoryValues[0x40] = 0x40;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void BIT_ZeroPage_Both_Negative_And_Overflow()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.MemoryValues[0x50] = 0xC0;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void BIT_Absolute_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2C, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x55;
        jit.TestHal.MemoryValues[0x3000] = 0xAA;
        jit.TestHal.Flags[CpuStatusFlags.Carry] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x55);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
    }

    [Fact]
    public void BIT_Absolute_Clear_All_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2C, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x0F;
        jit.TestHal.MemoryValues[0x1234] = 0x0F;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x0F);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void BIT_Absolute_Pattern_Test()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2C, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80;
        jit.TestHal.MemoryValues[0x4FFF] = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void BIT_Test_Specific_Bits()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x01;
        jit.TestHal.MemoryValues[0x80] = 0xE1;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x01);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
    }

    [Fact]
    public void BIT_Zero_Memory_Value()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x24);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x24, 0x90],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.TestHal.MemoryValues[0x90] = 0x00;
        jit.TestHal.Flags[CpuStatusFlags.Negative] = true;
        jit.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        jit.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeFalse();
        jit.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}