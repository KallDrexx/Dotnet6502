using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 PLP (Pull Processor Status) instruction
///
/// PLP pulls a value from the stack into the processor status register (flags) and:
/// - Affects ALL flags based on the pulled value
/// - Increments the stack pointer
/// - Does NOT affect registers
/// - Ignores the Break flag in the pulled value (real 6502 behavior)
/// </summary>
public class PlpTests
{
    [Fact]
    public void PLP_Basic()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags initially
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        // Push a value with specific flags set (Carry, InterruptDisable, Overflow)
        // 0x01 | 0x04 | 0x40 = 0x45
        jit.TestHal.PushToStack(0x45);
        jit.RunMethod(0x1234);

        // Check flags are set according to pulled value
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();      // bit 0
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();      // bit 1
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue(); // bit 2
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();   // bit 3
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();   // bit 6
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();  // bit 7
    }

    [Fact]
    public void PLP_All_Flags_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags initially
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        // Push value with all standard flags set
        // 0x01 | 0x02 | 0x04 | 0x08 | 0x40 | 0x80 = 0xCF
        jit.TestHal.PushToStack(0xCF);
        jit.RunMethod(0x1234);


        // All flags should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void PLP_All_Flags_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags initially
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        // Push value with all flags clear
        jit.TestHal.PushToStack(0x00);
        jit.RunMethod(0x1234);


        // All flags should be clear
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Fact]
    public void PLP_Mixed_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);

        // Start with opposite pattern
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        // Push value with mixed flags: Zero and Negative set
        // 0x02 | 0x80 = 0x82
        jit.TestHal.PushToStack(0x82);
        jit.RunMethod(0x1234);


        // Check specific flag pattern
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Fact]
    public void PLP_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, nesIrInstructions);
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;

        jit.TestHal.PushToStack(0xFF);
        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)0x42); // Should remain unchanged
        jit.TestHal.XRegister.ShouldBe((byte)0x33); // Should remain unchanged
        jit.TestHal.YRegister.ShouldBe((byte)0x77); // Should remain unchanged
    }

    [Fact]
    public void PLP_Edge_Cases()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x28);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x28],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test with just carry flag
        var jit1 = TestJitCompiler.Create();
            jit1.AddMethod(0x1234, nesIrInstructions);
        jit1.TestHal.PushToStack(0x01); // Just carry flag
        jit1.RunMethod(0x1234);
        jit1.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit1.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();

        // Test with alternating pattern
        var jit2 = TestJitCompiler.Create();
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.PushToStack(0xAA); // 10101010 pattern
        jit2.RunMethod(0x1234);
        jit2.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue(); // bit 1
        jit2.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue(); // bit 3
        jit2.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue(); // bit 7
    }
}