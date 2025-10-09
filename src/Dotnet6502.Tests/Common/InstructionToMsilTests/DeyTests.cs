using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 DEY (Decrement Y Register) instruction
///
/// 6502 DEY Behavior:
/// - Decrements Y register by 1
/// - Sets Zero flag if result is 0x00
/// - Sets Negative flag if bit 7 of result is set
/// - Does NOT affect Carry or Overflow flags
/// - Wraparound behavior: 0x00 - 1 = 0xFF
/// </summary>
public class DeyTests
{
    [Fact]
    public void DEY_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x05;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x04);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x01; // 0x01 - 1 = 0x00
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x00);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_Wraparound_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x00; // 0x00 - 1 = 0xFF (wraparound)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFF);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_Negative_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x81; // 0x81 - 1 = 0x80 (still negative)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x80);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_Positive_Non_Zero_Result()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x42;
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x41);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_From_Maximum_Positive()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x7F; // 127, decrement to 126 (0x7E, still positive)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7E);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_From_Negative_To_Negative()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0xFE; // -2, decrement to -3 (0xFD)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0xFD);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }

    [Fact]
    public void DEY_Boundary_Value_0x80()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x88);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x88],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.YRegister = 0x80; // -128, decrement to 127 (0x7F, becomes positive)
        jit.RunMethod(0x1234);

        jit.TestHal.YRegister.ShouldBe((byte)0x7F);
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }
}