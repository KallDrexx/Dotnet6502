using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CMP (Compare Accumulator) instruction
///
/// CMP subtracts a memory value or immediate value from the accumulator,
/// but does NOT store the result. It only affects the flags.
///
/// Flags affected:
/// - Carry: Set if A >= memory value (no borrow needed)
/// - Zero: Set if A == memory value
/// - Negative: Set if bit 7 of (A - memory value) is set
/// - Overflow flag is NOT affected
/// </summary>
public class CmpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Immediate_Equal(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0x42],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Immediate_Greater(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x50;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x50);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Immediate_Less(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0x50],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x30;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x30);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Immediate_Zero_Compare(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0x00],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_ZeroPage_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC5, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x7F;
        jit.Memory.MemoryBlock[0x10] = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_ZeroPageX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD5, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x10;
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x20;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x10);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_ZeroPageX_Wraparound(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD5);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD5, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x80;
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x70;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Absolute_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xCD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xCD, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.Memory.MemoryBlock[0x3000] = 0x01;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_AbsoluteX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xDD);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xDD, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x42;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_AbsoluteY_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD9, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x00;
        jit.TestHal.YRegister = 0x01;
        jit.Memory.MemoryBlock[0x5000] = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Signed_Comparison(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0x80],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x7F;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CMP_Boundary_Values(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC9);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC9, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0xFF;
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}
