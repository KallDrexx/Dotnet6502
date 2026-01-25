using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 SEC (Set Carry Flag) instruction
///
/// SEC sets the carry flag and:
/// - Only affects the carry flag (sets it to 1)
/// - Does NOT affect any other flags
/// - Does NOT affect any registers
/// </summary>
public class SecTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_Basic(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear carry flag initially
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.RunMethod(0x1234);

        // Carry flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_When_Already_Set(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Carry flag already set
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.RunMethod(0x1234);

        // Carry flag should remain set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_Preserves_Other_Flags(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set all flags except carry
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);

        jit.RunMethod(0x1234);

        // Only carry flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();

        // All other flags should be preserved
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_Does_Not_Affect_Registers(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);

        jit.RunMethod(0x1234);

        // Registers should remain unchanged
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);

        // Only carry flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_With_Mixed_Initial_Flags(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Set a mixed pattern of flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false); // Will be set
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false); // Should remain false
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);  // Should remain true
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false); // Should remain false
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);  // Should remain true
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false); // Should remain false

        jit.RunMethod(0x1234);

        // Only carry flag should be affected
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();

        // All other flags should remain unchanged
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_Preserves_Clear_Flags(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear all flags
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, false);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, false);

        jit.RunMethod(0x1234);

        // Only carry flag should be set
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();

        // All other flags should remain clear
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SEC_Multiple_Calls(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x38);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x38],
        };

        var context = new InstructionConverter.Context(
            new Dictionary<ushort, string>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);
        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.AddMethod(0x1234, nesIrInstructions);

        // Clear carry flag initially
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, false);

        // First SEC call
        jit.RunMethod(0x1234);
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();

        // Second SEC call (should have no effect)
        var jit2 = TestJitCompiler.Create();
        jit2.AlwaysUseInterpreter = useInterpreter;
            jit2.AddMethod(0x1234, nesIrInstructions);
        jit2.TestHal.SetFlag(CpuStatusFlags.Carry, true); // Already set
        jit2.RunMethod(0x1234);
        jit2.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
    }
}
