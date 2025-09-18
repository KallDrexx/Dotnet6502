using DotNesJit.Common;
using DotNesJit.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 BCC (Branch if Carry Clear) instruction
///
/// BCC branches when the carry flag is clear (0):
/// - Branches when carry flag = 0
/// - Does not branch when carry flag = 1
/// - Uses relative addressing with signed 8-bit offset (-128 to +127)
/// - Does NOT affect any flags
/// - Does NOT affect any registers
/// </summary>
public class BccTests
{
    [Fact]
    public void BCC_Branches_When_Carry_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<NesIr.Instruction>
        {
            // Set up carry flag as clear
            new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Carry)),

            // Add the BCC instruction
            nesIrInstructions[0],

            // Instruction that should be skipped if branch is taken
            new NesIr.Copy(new NesIr.Constant(99), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("branch_target")),

            // Instruction that should be executed at branch target
            new NesIr.Copy(new NesIr.Constant(42), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false; // Carry clear
        testRunner.RunTestMethod();

        // Branch should be taken, skipping X register assignment
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        testRunner.NesHal.ARegister.ShouldBe((byte)42); // Should be executed at target
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse(); // Should remain unchanged
    }

    [Fact]
    public void BCC_Does_Not_Branch_When_Carry_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<NesIr.Instruction>
        {
            // Set up carry flag as set
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Carry)),

            // Add the BCC instruction
            nesIrInstructions[0],

            // Instruction that should be executed if branch is NOT taken
            new NesIr.Copy(new NesIr.Constant(77), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("branch_target")),

            // Instruction that should be skipped if branch is not taken
            new NesIr.Copy(new NesIr.Constant(88), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true; // Carry set
        testRunner.RunTestMethod();

        // Branch should NOT be taken, continuing to next instruction
        testRunner.NesHal.XRegister.ShouldBe((byte)77); // Should be executed
        testRunner.NesHal.ARegister.ShouldBe((byte)88); // Should also be executed
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // Should remain unchanged
    }

    [Fact]
    public void BCC_Backward_Branch_When_Carry_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0xFB], // Branch backward by -5 (0xFB = -5 in signed byte)
            TargetAddress = 0x7FFA // Target address for backward branch
        };

        var labels = new Dictionary<ushort, string> { { 0x7FFA, "loop_start" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup for a loop-like scenario
        var allInstructions = new List<NesIr.Instruction>
        {
            // Initialize counter
            new NesIr.Copy(new NesIr.Constant(0), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Loop target
            new NesIr.Label(new NesIr.Identifier("loop_start")),

            // Increment counter
            new NesIr.Binary(
                NesIr.BinaryOperator.Add,
                new NesIr.Register(NesIr.RegisterName.XIndex),
                new NesIr.Constant(1),
                new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Check if we should continue (clear carry if X < 3)
            new NesIr.Binary(
                NesIr.BinaryOperator.LessThan,
                new NesIr.Register(NesIr.RegisterName.XIndex),
                new NesIr.Constant(3),
                new NesIr.Variable(0)),
            new NesIr.Copy(
                new NesIr.Variable(0),
                new NesIr.Flag(NesIr.FlagName.Carry)),
            new NesIr.Binary(
                NesIr.BinaryOperator.Xor,
                new NesIr.Flag(NesIr.FlagName.Carry),
                new NesIr.Constant(1),
                new NesIr.Flag(NesIr.FlagName.Carry)), // Flip carry (1 becomes 0, 0 becomes 1)

            // Add the BCC instruction (will branch if carry is clear)
            nesIrInstructions[0]
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Loop should execute 3 times before carry becomes set
        testRunner.NesHal.XRegister.ShouldBe((byte)3);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue(); // Final state: carry set (loop exit condition)
    }

    [Fact]
    public void BCC_Does_Not_Affect_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Set all flags to known state
            new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Carry)), // Clear carry for branch
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Zero)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Negative)),
            new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Overflow)),

            // Add the BCC instruction
            nesIrInstructions[0],

            // Target label
            new NesIr.Label(new NesIr.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial flag states
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // BCC should not affect any flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeFalse();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
    }

    [Fact]
    public void BCC_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Set carry clear for branch to occur
            new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Carry)),

            // Add the BCC instruction
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
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;

        testRunner.RunTestMethod();

        // BCC should not affect any registers
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void BCC_Forward_Branch_Maximum_Positive_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x7F], // Maximum forward branch (+127)
            TargetAddress = 0x8081 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x8081, "far_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Set carry clear
            new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Carry)),

            // Add the BCC instruction
            nesIrInstructions[0],

            // This should be skipped
            new NesIr.Copy(new NesIr.Constant(111), new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Target label
            new NesIr.Label(new NesIr.Identifier("far_target")),

            // This should be executed
            new NesIr.Copy(new NesIr.Constant(222), new NesIr.Register(NesIr.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = false;
        testRunner.RunTestMethod();

        // Branch should be taken
        testRunner.NesHal.XRegister.ShouldBe((byte)0); // Should be skipped
        testRunner.NesHal.ARegister.ShouldBe((byte)222); // Should be executed
    }

    [Fact]
    public void BCC_Backward_Branch_Maximum_Negative_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x80], // Maximum backward branch (-128)
            TargetAddress = 0x7F82 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x7F82, "back_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<NesIr.Instruction>
        {
            // Target label (at beginning for backward branch)
            new NesIr.Label(new NesIr.Identifier("back_target")),

            // Mark that we reached the target
            new NesIr.Copy(new NesIr.Constant(155), new NesIr.Register(NesIr.RegisterName.Accumulator)),

            // Set up a condition to branch only once
            new NesIr.Binary(
                NesIr.BinaryOperator.Equals,
                new NesIr.Register(NesIr.RegisterName.XIndex),
                new NesIr.Constant(0),
                new NesIr.Variable(0)),

            // Set carry based on condition (clear if X == 0)
            new NesIr.Binary(
                NesIr.BinaryOperator.Xor,
                new NesIr.Variable(0),
                new NesIr.Constant(1),
                new NesIr.Flag(NesIr.FlagName.Carry)),

            // Increment X to prevent infinite loop
            new NesIr.Binary(
                NesIr.BinaryOperator.Add,
                new NesIr.Register(NesIr.RegisterName.XIndex),
                new NesIr.Constant(1),
                new NesIr.Register(NesIr.RegisterName.XIndex)),

            // Add the BCC instruction
            nesIrInstructions[0]
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.NesHal.XRegister = 0; // Start with 0 to trigger branch once
        testRunner.RunTestMethod();

        // Should have branched back, loop executes twice (X starts at 0, branches when X==0, then X becomes 1, then X becomes 2, then carry is set and no more branch)
        testRunner.NesHal.ARegister.ShouldBe((byte)155); // Target reached
        testRunner.NesHal.XRegister.ShouldBe((byte)2); // Incremented twice (loop executes twice)
    }

    [Fact]
    public void BCC_With_Various_Carry_States_From_Previous_Operations()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x90);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x90, 0x03],
            TargetAddress = 0x8005
        };

        var labels = new Dictionary<ushort, string> { { 0x8005, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test case 1: After operation that sets carry
        {
            var allInstructions = new List<NesIr.Instruction>
            {
                // Simulate operation that sets carry flag (like overflow addition)
                new NesIr.Copy(new NesIr.Constant(1), new NesIr.Flag(NesIr.FlagName.Carry)),

                // Add the BCC instruction
                nesIrInstructions[0],

                // This should execute (branch NOT taken)
                new NesIr.Copy(new NesIr.Constant(50), new NesIr.Register(NesIr.RegisterName.XIndex)),

                // Target label
                new NesIr.Label(new NesIr.Identifier("target")),

                // This should also execute
                new NesIr.Copy(new NesIr.Constant(100), new NesIr.Register(NesIr.RegisterName.Accumulator))
            };

            var testRunner1 = new InstructionTestRunner(allInstructions);
            testRunner1.NesHal.Flags[CpuStatusFlags.Carry] = true;
            testRunner1.RunTestMethod();

            // Branch should NOT be taken
            testRunner1.NesHal.XRegister.ShouldBe((byte)50);
            testRunner1.NesHal.ARegister.ShouldBe((byte)100);
        }

        // Test case 2: After operation that clears carry
        {
            var allInstructions = new List<NesIr.Instruction>
            {
                // Simulate operation that clears carry flag
                new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Carry)),

                // Add the BCC instruction
                nesIrInstructions[0],

                // This should be skipped (branch taken)
                new NesIr.Copy(new NesIr.Constant(75), new NesIr.Register(NesIr.RegisterName.XIndex)),

                // Target label
                new NesIr.Label(new NesIr.Identifier("target")),

                // This should execute
                new NesIr.Copy(new NesIr.Constant(150), new NesIr.Register(NesIr.RegisterName.Accumulator))
            };

            var testRunner2 = new InstructionTestRunner(allInstructions);
            testRunner2.NesHal.Flags[CpuStatusFlags.Carry] = false;
            testRunner2.RunTestMethod();

            // Branch SHOULD be taken
            testRunner2.NesHal.XRegister.ShouldBe((byte)0); // Skipped
            testRunner2.NesHal.ARegister.ShouldBe((byte)150); // Executed at target
        }
    }
}