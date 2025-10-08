using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CPY (Compare Y Register) instruction
///
/// CPY subtracts a memory value or immediate value from the Y register,
/// but does NOT store the result. It only affects the flags.
///
/// Flags affected:
/// - Carry: Set if Y >= memory value (no borrow needed)
/// - Zero: Set if Y == memory value
/// - Negative: Set if bit 7 of (Y - memory value) is set
/// - Overflow flag is NOT affected
///
/// Note: CPY supports fewer addressing modes than CMP (Immediate, ZeroPage, Absolute only)
/// </summary>
public class CpyTests
{
    [Fact]
    public void CPY_Immediate_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x42;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void CPY_Immediate_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x50;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x50);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPY_Immediate_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x30;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x30);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPY_Immediate_Zero_Compare()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPY_ZeroPage_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x7F;
        jit.MemoryMap.MemoryBlock[0x10] = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPY_ZeroPage_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x90;
        jit.MemoryMap.MemoryBlock[0x20] = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x90);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPY_ZeroPage_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC4, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x10;
        jit.MemoryMap.MemoryBlock[0x30] = 0x20;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x10);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPY_Absolute_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xAA;
        jit.MemoryMap.MemoryBlock[0x3000] = 0xAA;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xAA);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPY_Absolute_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;
        jit.MemoryMap.MemoryBlock[0x1234] = 0x01;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPY_Absolute_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCC, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.MemoryMap.MemoryBlock[0x4FFF] = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPY_Signed_Comparison()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPY_Boundary_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC0, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}