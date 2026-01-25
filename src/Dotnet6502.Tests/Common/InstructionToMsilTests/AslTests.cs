using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ASL (Arithmetic Shift Left) instruction
///
/// ASL shifts all bits left one position. Bit 0 is filled with 0.
/// The original bit 7 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 7 before the shift
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// </summary>
public class AslTests
{
    // Note: ASL Accumulator mode tests are not included as the InstructionConverter
    // does not yet support the Accumulator addressing mode for shift instructions

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_ZeroPage_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x06);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x06, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x10] = 0x20;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0x40);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_ZeroPage_Carry_And_Negative(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x06);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x06, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x20] = 0xC0;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x20].ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_ZeroPageX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x16);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x16, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x15;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x35].ShouldBe((byte)0x2A);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_ZeroPageX_Wraparound(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x16);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x16, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x33;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x01].ShouldBe((byte)0x66);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_Absolute_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x0E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x0E, 0x00, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x3000] = 0x42;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x3000].ShouldBe((byte)0x84);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_AbsoluteX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x7F;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x200F].ShouldBe((byte)0xFE);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ASL_AbsoluteX_Carry_Zero(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x1E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x1E, 0xFF, 0x4F],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x01;
        jit.Memory.MemoryBlock[0x5000] = 0x80;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x5000].ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }
}
