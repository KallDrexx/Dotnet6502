using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 CLV (Clear Overflow Flag) instruction
///
/// CLV clears the overflow flag and:
/// - Only affects the overflow flag (sets it to 0)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class ClvTests
{
    [Fact]
    public void CLV_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set overflow flag initially
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        // Overflow flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void CLV_When_Already_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Overflow flag already clear
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = false;
        testRunner.RunTestMethod();

        // Overflow flag should remain clear
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void CLV_Preserves_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Set all flags
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;

        testRunner.RunTestMethod();

        // Only overflow flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();

        // All other flags should be preserved
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
    }

    [Fact]
    public void CLV_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xB8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;

        testRunner.RunTestMethod();

        // Registers should remain unchanged
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only overflow flag should be cleared
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }

    [Fact]
    public void CLV_No_Set_Instruction()
    {
        // Note: The 6502 has no "Set Overflow" instruction
        // The overflow flag can only be set by arithmetic operations (ADC, SBC)
        // or pulled from the stack (PLP, RTI)
        // This test verifies CLV works regardless of how overflow was set
        
        var instructionInfo = InstructionSet.GetInstruction(0xB8);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xB8],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>(),
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var testRunner = new InstructionTestRunner(nesIrInstructions);

        // Manually set overflow flag (simulating an arithmetic operation result)
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.RunTestMethod();

        // CLV should clear it regardless of how it was set
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeFalse();
    }
}