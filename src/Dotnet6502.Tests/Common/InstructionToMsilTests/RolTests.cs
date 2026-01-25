using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 ROL (Rotate Left through Carry) instruction
///
/// ROL shifts all bits left one position. The Carry flag is shifted into bit 0.
/// The original bit 7 is shifted into the Carry flag.
///
/// Flags affected:
/// - Carry: Set to the value of bit 7 before the rotation
/// - Zero: Set if result is 0
/// - Negative: Set if bit 7 of result is set
/// </summary>
public class RolTests
{
    // Note: ROL Accumulator mode tests are not included as the InstructionConverter
    // does not yet support the Accumulator addressing mode for shift instructions

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_ZeroPage_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x26);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x26, 0x10],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x10] = 0x55;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x10].ShouldBe((byte)0xAA);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_ZeroPage_Carry_In(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x26);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x26, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.Memory.MemoryBlock[0x20] = 0x7E;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x20].ShouldBe((byte)0xFD);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_ZeroPageX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x36);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x36, 0x30],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x05;
        jit.Memory.MemoryBlock[0x35] = 0x81;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x35].ShouldBe((byte)0x02);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_ZeroPageX_Wraparound(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x36);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x36, 0xFF],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x02;
        jit.Memory.MemoryBlock[0x01] = 0x33;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x01].ShouldBe((byte)0x67);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_Absolute_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x2E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x2E, 0x00, 0x30],
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
    public void ROL_AbsoluteX_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x3E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x3E, 0x00, 0x20],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.XRegister = 0x0F;
        jit.Memory.MemoryBlock[0x200F] = 0x01;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        jit.Memory.MemoryBlock[0x200F].ShouldBe((byte)0x03);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ROL_AbsoluteX_Zero_Result(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x3E);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x3E, 0xFF, 0x4F],
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
