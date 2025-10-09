using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CLI (Clear Interrupt Disable Flag) instruction
///
/// CLI clears the interrupt disable flag and:
/// - Only affects the interrupt disable flag (sets it to 0)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class CliTests
{
    [Fact]
    public void CLI_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x58);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x58],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set interrupt disable flag initially
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.RunMethod(0x1234);

        // Interrupt disable flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
    }

    [Fact]
    public void CLI_When_Already_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x58);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x58],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Interrupt disable flag already clear
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.RunMethod(0x1234);

        // Interrupt disable flag should remain clear
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
    }

    [Fact]
    public void CLI_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x58);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x58],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // Only interrupt disable flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();

        // All other flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void CLI_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x58);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x58],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only interrupt disable flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
    }
}