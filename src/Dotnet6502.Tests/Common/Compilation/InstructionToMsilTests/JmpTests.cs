using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 JMP (Jump) instruction
///
/// JMP unconditionally jumps to the target address:
/// - JMP Absolute (0x4C): Direct jump to 16-bit absolute address
/// - JMP Indirect (0x6C): Jump to address stored at 16-bit address
/// - Does NOT affect any flags
/// - Does NOT affect any registers
/// - Unlike branch instructions, JMP can reach any address in memory
/// </summary>
public class JmpTests
{
    [Fact]
    public void JMP_Absolute_Basic_Jump()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0x00, 0x90], // JMP $9000
            TargetAddress = 0x9000 // Target address for jump
        };

        var labels = new Dictionary<ushort, string> { { 0x9000, "jump_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the jump
        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // Instruction that should be skipped (never reached)
            new NesIr.Copy(new NesIr.Constant(99), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("jump_target")),

            // Instruction that should be executed at jump target
            new NesIr.Copy(new NesIr.Constant(42), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Jump should always be taken, skipping X register assignment
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        testRunner.NesHal.ARegister.ShouldBe((byte)42); // Should be executed at target
    }

    [Fact]
    public void JMP_Indirect_Basic_Jump()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6C, 0x20, 0x80], // JMP ($8020) - indirect jump
            TargetAddress = 0x9000 // The actual target address (stored at $8020)
        };

        var labels = new Dictionary<ushort, string> { { 0x9000, "jump_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the jump
        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // Instruction that should be skipped (never reached)
            new NesIr.Copy(new NesIr.Constant(99), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("jump_target")),

            // Instruction that should be executed at jump target
            new NesIr.Copy(new NesIr.Constant(77), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Jump should always be taken, skipping X register assignment
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        testRunner.NesHal.ARegister.ShouldBe((byte)77); // Should be executed at target
    }

    [Fact]
    public void JMP_Absolute_Does_Not_Affect_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0x00, 0x85],
            TargetAddress = 0x8500
        };

        var labels = new Dictionary<ushort, string> { { 0x8500, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Set all flags to known state
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Carry)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Zero)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Negative)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Overflow)),

            // Add the JMP instruction
            nesIrInstructions[0],

            // Target label
            new NesIr.Label(new NesIr.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial flag states
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // JMP should not affect any flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
    }

    [Fact]
    public void JMP_Indirect_Does_Not_Affect_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6C, 0x30, 0x81],
            TargetAddress = 0x8500
        };

        var labels = new Dictionary<ushort, string> { { 0x8500, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Set all flags to known state
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Carry)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Zero)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Negative)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Overflow)),

            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // Target label
            new NesIr.Label(new NesIr.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial flag states
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // JMP should not affect any flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
    }

    [Fact]
    public void JMP_Absolute_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0x00, 0x86],
            TargetAddress = 0x8600
        };

        var labels = new Dictionary<ushort, string> { { 0x8600, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // Target label
            new NesIr.Label(new NesIr.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial register values
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.NesHal.StackPointer = 0xFF;

        testRunner.RunTestMethod();

        // JMP should not affect any registers
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void JMP_Indirect_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6C, 0x40, 0x82],
            TargetAddress = 0x8600
        };

        var labels = new Dictionary<ushort, string> { { 0x8600, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // Target label
            new NesIr.Label(new NesIr.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial register values
        testRunner.NesHal.ARegister = 0x55;
        testRunner.NesHal.XRegister = 0xAA;
        testRunner.NesHal.YRegister = 0xBB;
        testRunner.NesHal.StackPointer = 0xCC;

        testRunner.RunTestMethod();

        // JMP should not affect any registers
        testRunner.NesHal.ARegister.ShouldBe((byte)0x55);
        testRunner.NesHal.XRegister.ShouldBe((byte)0xAA);
        testRunner.NesHal.YRegister.ShouldBe((byte)0xBB);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xCC);
    }

    [Fact]
    public void JMP_Absolute_Far_Address()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0xFF, 0xFF], // JMP $FFFF (far address)
            TargetAddress = 0xFFFF
        };

        var labels = new Dictionary<ushort, string> { { 0xFFFF, "far_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // This should be skipped
            new NesIr.Copy(new NesIr.Constant(111), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("far_target")),

            // This should be executed
            new NesIr.Copy(new NesIr.Constant(222), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Jump should be taken to far address
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should be skipped
        testRunner.NesHal.ARegister.ShouldBe((byte)222); // Should be executed
    }

    [Fact]
    public void JMP_Indirect_Different_Memory_Locations()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6C, 0x00, 0x03], // JMP ($0300) - zero page indirect
            TargetAddress = 0xA000
        };

        var labels = new Dictionary<ushort, string> { { 0xA000, "zp_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // This should be skipped
            new NesIr.Copy(new NesIr.Constant(123), new NesIr.Register(NesIr.RegisterName.YIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("zp_target")),

            // This should be executed
            new NesIr.Copy(new NesIr.Constant(210), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Jump should be taken via indirect addressing
        testRunner.NesHal.YRegister.ShouldBe((byte)0); // Should be skipped
        testRunner.NesHal.ARegister.ShouldBe((byte)210); // Should be executed
    }

    [Fact]
    public void JMP_Absolute_Low_Address()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0x00, 0x00], // JMP $0000 (lowest address)
            TargetAddress = 0x0000
        };

        var labels = new Dictionary<ushort, string> { { 0x0000, "low_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // This should be skipped
            new NesIr.Copy(new NesIr.Constant(88), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("low_target")),

            // This should be executed
            new NesIr.Copy(new NesIr.Constant(99), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Jump should be taken to low address
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should be skipped
        testRunner.NesHal.ARegister.ShouldBe((byte)99); // Should be executed
    }

    [Fact]
    public void JMP_Instructions_Are_Unconditional()
    {
        // Test that JMP works regardless of flag states
        var instructionInfo = InstructionSet.GetInstruction(0x4C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x4C, 0x50, 0x87],
            TargetAddress = 0x8750
        };

        var labels = new Dictionary<ushort, string> { { 0x8750, "always_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test with all flags set to different states
        foreach (bool carryState in new[] { true, false })
        foreach (bool zeroState in new[] { true, false })
        foreach (bool negativeState in new[] { true, false })
        foreach (bool overflowState in new[] { true, false })
        {
            var allInstructions = new List<NesIr.Instruction>
            {
                // Set up various flag states
                new NesIr.Copy(new NesIr.Constant((byte)(carryState ? 1 : 0)), new NesIr.Flag(NesIr.FlagName.Carry)),
                new NesIr.Copy(new NesIr.Constant((byte)(zeroState ? 1 : 0)), new NesIr.Flag(NesIr.FlagName.Zero)),
                new NesIr.Copy(new NesIr.Constant((byte)(negativeState ? 1 : 0)), new NesIr.Flag(NesIr.FlagName.Negative)),
                new NesIr.Copy(new NesIr.Constant((byte)(overflowState ? 1 : 0)), new NesIr.Flag(NesIr.FlagName.Overflow)),

                // Add the JMP instruction
                nesIrInstructions[0],

                // This should always be skipped
                new NesIr.Copy(new NesIr.Constant(44), new NesIr.Register(NesIr.RegisterName.XIndex)),

                // Target label
                new NesIr.Label(new NesIr.Identifier("always_target")),

                // This should always be executed
                new NesIr.Copy(new NesIr.Constant(55), new NesIr.Register(NesIr.RegisterName.Accumulator))
            };

            var testRunner = new InstructionTestRunner(allInstructions);

            // Set flag states
            testRunner.NesHal.Flags[CpuStatusFlags.Carry] = carryState;
            testRunner.NesHal.Flags[CpuStatusFlags.Zero] = zeroState;
            testRunner.NesHal.Flags[CpuStatusFlags.Negative] = negativeState;
            testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = overflowState;

            testRunner.RunTestMethod();

            // JMP should always jump, regardless of flag states
            testRunner.NesHal.XRegister.ShouldBe((byte)0,
                $"Jump should always be taken (C:{carryState}, Z:{zeroState}, N:{negativeState}, V:{overflowState})");
            testRunner.NesHal.ARegister.ShouldBe((byte)55,
                $"Target should always be reached (C:{carryState}, Z:{zeroState}, N:{negativeState}, V:{overflowState})");
        }
    }
}