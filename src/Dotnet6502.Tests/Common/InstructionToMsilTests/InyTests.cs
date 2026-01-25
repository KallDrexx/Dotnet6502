using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 INY (Increment Y Register) instruction
///
/// 6502 INY Behavior:
/// - Increments Y register by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0xFF + 1 = 0x00
/// </summary>
public class InyTests
{
    [Fact]
    public void INY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x05;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x06);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFF; // 0xFF + 1 = 0x00 (wraparound)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x7F; // 127, increment to 128 (0x80, negative)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_Positive_Non_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x42;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x43);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_From_Zero()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x01);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_From_Negative_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x80; // -128, increment to -127 (0x81)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x81);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_Boundary_Value_0x7E()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x7E; // 126, increment to 127 (0x7F, still positive)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void INY_Negative_To_More_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xC8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xC8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFE; // -2, increment to -1 (0xFF)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }
}