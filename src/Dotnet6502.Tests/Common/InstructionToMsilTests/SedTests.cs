using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SED (Set Decimal Flag) instruction
///
/// SED sets the decimal flag and:
/// - Only affects the decimal flag (sets it to 1)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class SedTests
{
    [Fact]
    public void SED_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear decimal flag initially
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.RunMethod(0x1234);

        // Decimal flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
    }

    [Fact]
    public void SED_When_Already_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Decimal flag already set
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.RunMethod(0x1234);

        // Decimal flag should remain set
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
    }

    [Fact]
    public void SED_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags except decimal
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // Only decimal flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();

        // All other flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void SED_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xF8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xF8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(), []);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only decimal flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
    }
}