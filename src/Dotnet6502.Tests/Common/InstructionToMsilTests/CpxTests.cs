using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CPX (Compare X Register) instruction
///
/// CPX subtracts a memory value or immediate value from the X register,
/// but does NOT store the result. It only affects the flags.
///
/// Flags affected:
/// - Carry: Set if X >= memory value (no borrow needed)
/// - Zero: Set if X == memory value
/// - Negative: Set if bit 7 of (X - memory value) is set
/// - Overflow flag is NOT affected
///
/// Note: CPX supports fewer addressing modes than CMP (Immediate, ZeroPage, Absolute only)
/// </summary>
public class CpxTests
{
    [Fact]
    public void CPX_Immediate_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x42;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Fact]
    public void CPX_Immediate_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x50;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x50);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPX_Immediate_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x30;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x30);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPX_Immediate_Zero_Compare()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPX_ZeroPage_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE4, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x7F;
        jit.MemoryMap.MemoryBlock[0x10] = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPX_ZeroPage_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE4, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x90;
        jit.MemoryMap.MemoryBlock[0x20] = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x90);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPX_ZeroPage_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE4);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE4, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x10;
        jit.MemoryMap.MemoryBlock[0x30] = 0x20;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x10);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPX_Absolute_Equal()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xEC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xEC, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xAA;
        jit.MemoryMap.MemoryBlock[0x3000] = 0xAA;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xAA);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPX_Absolute_Greater()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xEC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xEC, 0x34, 0x12],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.MemoryMap.MemoryBlock[0x1234] = 0x01;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPX_Absolute_Less()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xEC);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xEC, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x00;
        jit.MemoryMap.MemoryBlock[0x4FFF] = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void CPX_Signed_Comparison()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CPX_Boundary_Values()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xE0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xE0, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.XRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}