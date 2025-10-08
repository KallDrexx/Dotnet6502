using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set overflow flag initially
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        // Overflow flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Overflow flag already clear
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.RunMethod(0x1234);

        // Overflow flag should remain clear
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // Only overflow flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();

        // All other flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only overflow flag should be cleared
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
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
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Manually set overflow flag (simulating an arithmetic operation result)
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        // CLV should clear it regardless of how it was set
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
    }
}