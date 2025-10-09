using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 BVS (Branch if Overflow Set) instruction
///
/// BVS branches when the overflow flag is set (1):
/// - Branches when overflow flag = 1 (overflow occurred from previous operation)
/// - Does not branch when overflow flag = 0
/// - Uses relative addressing with signed 8-bit offset (-128 to +127)
/// - Does NOT affect any flags
/// - Does NOT affect any registers
/// </summary>
public class BvsTests
{
    [Fact]
    public void BVS_Branches_When_Overflow_Flag_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set up overflow flag as set (overflow occurred)
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the BVS instruction
            nesIrInstructions[0],

            // Instruction that should be skipped if branch is taken
            new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("branch_target")),

            // Instruction that should be executed at branch target
            new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true); // Overflow flag set
        jit.RunMethod(0x1234);

        // Branch should be taken, skipping X register assignment
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        jit.TestHal.ARegister.ShouldBe((byte)42); // Should be executed at target
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue(); // Should remain unchanged
    }

    [Fact]
    public void BVS_Does_Not_Branch_When_Overflow_Flag_Clear()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x05], // Branch forward by 5 bytes
            TargetAddress = 0x8007 // Target address for branch
        };

        var labels = new Dictionary<ushort, string> { { 0x8007, "branch_target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the branch
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set up overflow flag as clear (no overflow)
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the BVS instruction
            nesIrInstructions[0],

            // Instruction that should be executed if branch is NOT taken
            new Ir6502.Copy(new Ir6502.Constant(77), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("branch_target")),

            // Instruction that should be skipped if branch is not taken
            new Ir6502.Copy(new Ir6502.Constant(88), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, false); // Overflow flag clear
        jit.RunMethod(0x1234);

        // Branch should NOT be taken, continuing to next instruction
        jit.TestHal.XRegister.ShouldBe((byte)77); // Should be executed
        jit.TestHal.ARegister.ShouldBe((byte)88); // Should also be executed
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse(); // Should remain unchanged
    }

    [Fact]
    public void BVS_Backward_Branch_When_Overflow_Flag_Set()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0xFB], // Branch backward by -5 (0xFB = -5 in signed byte)
            TargetAddress = 0x7FFA // Target address for backward branch
        };

        var labels = new Dictionary<ushort, string> { { 0x7FFA, "loop_start" } };
        var context = new InstructionConverter.Context(
            labels);

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

            // Check if we should continue (set overflow flag if X < 3, clear overflow flag when X >= 3 to exit loop)
            new Ir6502.Binary(
                Ir6502.BinaryOperator.LessThan,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(3),
                new Ir6502.Flag(Ir6502.FlagName.Overflow)), // Set overflow flag when X < 3 (loop should continue)

            // Add the BVS instruction (will branch if overflow flag is set)
            nesIrInstructions[0]
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);
        jit.RunMethod(0x1234);

        // Loop should execute 3 times before overflow flag becomes clear (exit condition)
        jit.TestHal.XRegister.ShouldBe((byte)3);
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeFalse(); // Final state: overflow flag clear (loop exit condition)
    }

    [Fact]
    public void BVS_Does_Not_Affect_Other_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)), // Set overflow for branch
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),

            // Add the BVS instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial flag states
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);

        jit.RunMethod(0x1234);

        // BVS should not affect any flags
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
    }

    [Fact]
    public void BVS_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x02],
            TargetAddress = 0x8004
        };

        var labels = new Dictionary<ushort, string> { { 0x8004, "target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set overflow flag set for branch to occur
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the BVS instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial register values
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);

        jit.RunMethod(0x1234);

        // BVS should not affect any registers
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void BVS_Forward_Branch_Maximum_Positive_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x7F], // Maximum forward branch (+127)
            TargetAddress = 0x8081 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x8081, "far_target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set overflow flag set
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the BVS instruction
            nesIrInstructions[0],

            // This should be skipped
            new Ir6502.Copy(new Ir6502.Constant(111), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("far_target")),

            // This should be executed
            new Ir6502.Copy(new Ir6502.Constant(222), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.RunMethod(0x1234);

        // Branch should be taken
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should be skipped
        jit.TestHal.ARegister.ShouldBe((byte)222); // Should be executed
    }

    [Fact]
    public void BVS_Backward_Branch_Maximum_Negative_Offset()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x80], // Maximum backward branch (-128)
            TargetAddress = 0x7F82 // Target address
        };

        var labels = new Dictionary<ushort, string> { { 0x7F82, "back_target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Target label (at beginning for backward branch)
            new Ir6502.Label(new Ir6502.Identifier("back_target")),

            // Mark that we reached the target
            new Ir6502.Copy(new Ir6502.Constant(155), new Ir6502.Register(Ir6502.RegisterName.Accumulator)),

            // Set up a condition to branch only once
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Equals,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(0),
                new Ir6502.Flag(Ir6502.FlagName.Overflow)), // Set overflow flag if X == 0

            // Increment X to prevent infinite loop
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Add,
                new Ir6502.Register(Ir6502.RegisterName.XIndex),
                new Ir6502.Constant(1),
                new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Add the BVS instruction
            nesIrInstructions[0]
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);
        jit.TestHal.XRegister = 0; // Start with 0 to trigger branch once
        jit.RunMethod(0x1234);

        // Should have branched back, loop executes twice (X starts at 0, branches when X==0, then X becomes 1, then X becomes 2, then overflow is clear and no more branch)
        jit.TestHal.ARegister.ShouldBe((byte)155); // Target reached
        jit.TestHal.XRegister.ShouldBe((byte)2); // Incremented twice (loop executes twice)
    }

    [Fact]
    public void BVS_With_Various_Overflow_States_From_Previous_Operations()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x70);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x70, 0x03],
            TargetAddress = 0x8005
        };

        var labels = new Dictionary<ushort, string> { { 0x8005, "target" } };
        var context = new InstructionConverter.Context(
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test case 1: After operation that clears overflow flag (no overflow)
        {
            var allInstructions = new List<Ir6502.Instruction>
            {
                // Simulate operation that clears overflow flag
                new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

                // Add the BVS instruction
                nesIrInstructions[0],

                // This should execute (branch NOT taken)
                new Ir6502.Copy(new Ir6502.Constant(50), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

                // Target label
                new Ir6502.Label(new Ir6502.Identifier("target")),

                // This should also execute
                new Ir6502.Copy(new Ir6502.Constant(100), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            };

            var jit1 = TestJitCompiler.Create();
            jit1.AddMethod(0x1234, allInstructions);
            jit1.TestHal.SetFlag(CpuStatusFlags.Overflow, false);
            jit1.RunMethod(0x1234);

            // Branch should NOT be taken
            jit1.TestHal.XRegister.ShouldBe((byte)50);
            jit1.TestHal.ARegister.ShouldBe((byte)100);
        }

        // Test case 2: After operation that sets overflow flag (overflow occurred)
        {
            var allInstructions = new List<Ir6502.Instruction>
            {
                // Simulate operation that sets overflow flag
                new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

                // Add the BVS instruction
                nesIrInstructions[0],

                // This should be skipped (branch taken)
                new Ir6502.Copy(new Ir6502.Constant(75), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

                // Target label
                new Ir6502.Label(new Ir6502.Identifier("target")),

                // This should execute
                new Ir6502.Copy(new Ir6502.Constant(150), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            };

            var jit2 = TestJitCompiler.Create();
            jit2.AddMethod(0x1234, allInstructions);
            jit2.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
            jit2.RunMethod(0x1234);

            // Branch SHOULD be taken
            jit2.TestHal.XRegister.ShouldBe((byte)0); // Skipped
            jit2.TestHal.ARegister.ShouldBe((byte)150); // Executed at target
        }
    }
}