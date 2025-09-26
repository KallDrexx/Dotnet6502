using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 BNE (Branch if Not Equal) instruction
///
/// BNE branches when the zero flag is clear (0):
/// - Branches when zero flag = 0 (result of previous operation was not zero)
/// - Does not branch when zero flag = 1
/// - Uses relative addressing with signed 8-bit offset (-128 to +127)
/// - Does NOT affect any flags
/// - Does NOT affect any registers
/// </summary>
public class BneTests
{
    [Fact]
    public void BNE_Branches_When_Zero_Flag_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set up zero flag as clear (not equal condition)
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Zero)),

            // Add the BNE instruction
            nesIrInstructions[0],

            // Instruction that should be skipped if branch is taken
            new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("branch_target")),

            // Instruction that should be executed at branch target
            new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false; // Zero flag clear
        testRunner.RunTestMethod();

        // Branch should be taken, skipping X register assignment
        testRunner.TestHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        testRunner.TestHal.ARegister.ShouldBe((byte)42); // Should be executed at target
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse(); // Should remain unchanged
    }

    [Fact]
    public void BNE_Does_Not_Branch_When_Zero_Flag_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set up zero flag as set (equal condition)
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),

            // Add the BNE instruction
            nesIrInstructions[0],

            // Instruction that should be executed if branch is NOT taken
            new Ir6502.Copy(new Ir6502.Constant(77), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("branch_target")),

            // Instruction that should be skipped if branch is not taken
            new Ir6502.Copy(new Ir6502.Constant(88), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = true; // Zero flag set
        testRunner.RunTestMethod();

        // Branch should NOT be taken, continuing to next instruction
        testRunner.TestHal.XRegister.ShouldBe((byte)77); // Should be executed
        testRunner.TestHal.ARegister.ShouldBe((byte)88); // Should also be executed
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue(); // Should remain unchanged
    }

    [Fact]
    public void BNE_Backward_Branch_When_Zero_Flag_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0xFB], // Branch backward by -5 (0xFB = -5 in signed byte)
            TargetAddress = 0x7FFA // Target address for backward branch
        };

        var labels = new Dictionary<ushort, string> { { 0x7FFA, "loop_start" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup for a loop-like scenario
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Initialize counter
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Loop target
            new Ir6502.Label(new Ir6502.Identifier("loop_start")),

            // Increment counter
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Add,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(1),
                new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Check if we should continue (clear zero flag if X < 3, set zero flag when X >= 3 to exit loop)
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Equals,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(3),
                new Ir6502.Flag(Ir6502.FlagName.Zero)), // Set zero flag when X == 3 (loop should exit)

            // Add the BNE instruction (will branch if zero flag is clear)
            nesIrInstructions[0]
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.RunTestMethod();

        // Loop should execute 3 times before zero flag becomes set (exit condition)
        testRunner.TestHal.XRegister.ShouldBe((byte)3);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue(); // Final state: zero flag set (loop exit condition)
    }

    [Fact]
    public void BNE_Does_Not_Affect_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Zero)), // Clear zero for branch
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the BNE instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial flag states
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.TestHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // BNE should not affect any flags
        testRunner.TestHal.Flags[CpuStatusFlags.Zero].ShouldBeFalse();
        testRunner.TestHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.TestHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
    }

    [Fact]
    public void BNE_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set zero flag clear for branch to occur
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Zero)),

            // Add the BNE instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var testRunner = new InstructionTestRunner(allInstructions);

        // Set initial register values
        testRunner.TestHal.ARegister = 0x42;
        testRunner.TestHal.XRegister = 0x33;
        testRunner.TestHal.YRegister = 0x77;
        testRunner.TestHal.StackPointer = 0xFF;
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;

        testRunner.RunTestMethod();

        // BNE should not affect any registers
        testRunner.TestHal.ARegister.ShouldBe((byte)0x42);
        testRunner.TestHal.XRegister.ShouldBe((byte)0x33);
        testRunner.TestHal.YRegister.ShouldBe((byte)0x77);
        testRunner.TestHal.StackPointer.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void BNE_Forward_Branch_Maximum_Positive_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x7F], // Maximum forward branch (+127)
            TargetAddress = 0x8081 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x8081, "far_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Clear zero flag
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Zero)),

            // Add the BNE instruction
            nesIrInstructions[0],

            // This should be skipped
            new Ir6502.Copy(new Ir6502.Constant(111), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("far_target")),

            // This should be executed
            new Ir6502.Copy(new Ir6502.Constant(222), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.TestHal.Flags[CpuStatusFlags.Zero] = false;
        testRunner.RunTestMethod();

        // Branch should be taken
        testRunner.TestHal.XRegister.ShouldBe((byte)0); // Should be skipped
        testRunner.TestHal.ARegister.ShouldBe((byte)222); // Should be executed
    }

    [Fact]
    public void BNE_Backward_Branch_Maximum_Negative_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x80], // Maximum backward branch (-128)
            TargetAddress = 0x7F82 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x7F82, "back_target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Target label (at beginning for backward branch)
            new Ir6502.Label(new Ir6502.Identifier("back_target")),

            // Mark that we reached the target
            new Ir6502.Copy(new Ir6502.Constant(155), new Ir6502.Register(Ir6502.RegisterName.Accumulator)),

            // Set up a condition to branch only once - set zero flag when X >= 2 to stop branching
            new Ir6502.Binary(
                Ir6502.BinaryOperator.GreaterThanOrEqualTo,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(2),
                new Ir6502.Flag(Ir6502.FlagName.Zero)), // Set zero flag if X >= 2 to stop branching

            // Increment X to prevent infinite loop
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Add,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(1),
                new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Add the BNE instruction
            nesIrInstructions[0]
        };

        var testRunner = new InstructionTestRunner(allInstructions);
        testRunner.TestHal.XRegister = 0; // Start with 0 to trigger branch once
        testRunner.RunTestMethod();

        // Should have branched back, loop executes until zero flag becomes set (loop exit condition)
        testRunner.TestHal.ARegister.ShouldBe((byte)155); // Target reached
        testRunner.TestHal.XRegister.ShouldBe((byte)3); // Loop ran until X >= 2 set zero flag, then one more increment
    }

    [Fact]
    public void BNE_With_Various_Zero_States_From_Previous_Operations()
    {
        var instructionInfo = InstructionSet.GetInstruction(0xD0);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0xD0, 0x03],
            TargetAddress = 0x8005
        };

        var labels = new Dictionary<ushort, string> { { 0x8005, "target" } };
        var context = new InstructionConverter.Context(
            labels,
            new Dictionary<ushort, Function>());

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test case 1: After operation that sets zero flag (zero result)
        {
            var allInstructions = new List<Ir6502.Instruction>
            {
                // Simulate operation that sets zero flag
                new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),

                // Add the BNE instruction
                nesIrInstructions[0],

                // This should execute (branch NOT taken)
                new Ir6502.Copy(new Ir6502.Constant(50), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

                // Target label
                new Ir6502.Label(new Ir6502.Identifier("target")),

                // This should also execute
                new Ir6502.Copy(new Ir6502.Constant(100), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            };

            var testRunner1 = new InstructionTestRunner(allInstructions);
            testRunner1.TestHal.Flags[CpuStatusFlags.Zero] = true;
            testRunner1.RunTestMethod();

            // Branch should NOT be taken
            testRunner1.TestHal.XRegister.ShouldBe((byte)50);
            testRunner1.TestHal.ARegister.ShouldBe((byte)100);
        }

        // Test case 2: After operation that clears zero flag (non-zero result)
        {
            var allInstructions = new List<Ir6502.Instruction>
            {
                // Simulate operation that clears zero flag
                new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Zero)),

                // Add the BNE instruction
                nesIrInstructions[0],

                // This should be skipped (branch taken)
                new Ir6502.Copy(new Ir6502.Constant(75), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

                // Target label
                new Ir6502.Label(new Ir6502.Identifier("target")),

                // This should execute
                new Ir6502.Copy(new Ir6502.Constant(150), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            };

            var testRunner2 = new InstructionTestRunner(allInstructions);
            testRunner2.TestHal.Flags[CpuStatusFlags.Zero] = false;
            testRunner2.RunTestMethod();

            // Branch SHOULD be taken
            testRunner2.TestHal.XRegister.ShouldBe((byte)0); // Skipped
            testRunner2.TestHal.ARegister.ShouldBe((byte)150); // Executed at target
        }
    }
}