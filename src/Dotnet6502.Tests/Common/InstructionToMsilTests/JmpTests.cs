using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and target instructions around the jump
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // Instruction that should be skipped (never reached)
            new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("jump_target")),

            // Instruction that should be executed at jump target
            new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);
        jit.RunMethod(0x1234);

        // Jump should always be taken, skipping X register assignment
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        jit.TestHal.ARegister.ShouldBe((byte)42); // Should be executed at target
    }

    [Fact]
    public void JMP_Indirect_Basic_Jump()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x6C);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x6C, 0x20, 0x80], // JMP ($8020) - indirect jump
            TargetAddress = 0x8020
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());

        var allInstructions = InstructionConverter.Convert(instruction, context)
            .Append(
                // Instruction that should be skipped (never reached)
                new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex))
            )
            .ToArray();

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Set up method that will be jumped to
        jit.MemoryMap.MemoryBlock[0x8020] = 0x78;
        jit.MemoryMap.MemoryBlock[0x8021] = 0x56;
        jit.AddMethod(0x5678, [
            new Ir6502.Copy(new Ir6502.Constant(77), new Ir6502.Register(Ir6502.RegisterName.Accumulator)),
        ]);

        // Run
        jit.RunMethod(0x1234);

        // Jump should always be taken, skipping X register assignment
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should remain 0 (skipped)
        jit.TestHal.ARegister.ShouldBe((byte)77); // Should be executed at target
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the JMP instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial flag states
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);

        jit.RunMethod(0x1234);

        // JMP should not affect any flags
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial flag states
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);

        jit.RunMethod(0x1234);

        // JMP should not affect any flags
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial register values
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;

        jit.RunMethod(0x1234);

        // JMP should not affect any registers
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("target"))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Set initial register values
        jit.TestHal.ARegister = 0x55;
        jit.TestHal.XRegister = 0xAA;
        jit.TestHal.YRegister = 0xBB;
        jit.TestHal.StackPointer = 0xCC;

        jit.RunMethod(0x1234);

        // JMP should not affect any registers
        jit.TestHal.ARegister.ShouldBe((byte)0x55);
        jit.TestHal.XRegister.ShouldBe((byte)0xAA);
        jit.TestHal.YRegister.ShouldBe((byte)0xBB);
        jit.TestHal.StackPointer.ShouldBe((byte)0xCC);
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // This should be skipped
            new Ir6502.Copy(new Ir6502.Constant(111), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("far_target")),

            // This should be executed
            new Ir6502.Copy(new Ir6502.Constant(222), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);
        jit.RunMethod(0x1234);

        // Jump should be taken to far address
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should be skipped
        jit.TestHal.ARegister.ShouldBe((byte)222); // Should be executed
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP indirect instruction
            nesIrInstructions[0],

            // This should be skipped
            new Ir6502.Copy(new Ir6502.Constant(123), new Ir6502.Register(Ir6502.RegisterName.YIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("zp_target")),

            // This should be executed
            new Ir6502.Copy(new Ir6502.Constant(210), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);
        jit.RunMethod(0x1234);

        // Jump should be taken via indirect addressing
        jit.TestHal.YRegister.ShouldBe((byte)0); // Should be skipped
        jit.TestHal.ARegister.ShouldBe((byte)210); // Should be executed
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JMP instruction
            nesIrInstructions[0],

            // This should be skipped
            new Ir6502.Copy(new Ir6502.Constant(88), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Target label
            new Ir6502.Label(new Ir6502.Identifier("low_target")),

            // This should be executed
            new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);
        jit.RunMethod(0x1234);

        // Jump should be taken to low address
        jit.TestHal.XRegister.ShouldBe((byte)0); // Should be skipped
        jit.TestHal.ARegister.ShouldBe((byte)99); // Should be executed
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
            labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Test with all flags set to different states
        foreach (bool carryState in new[] { true, false })
        foreach (bool zeroState in new[] { true, false })
        foreach (bool negativeState in new[] { true, false })
        foreach (bool overflowState in new[] { true, false })
        {
            var allInstructions = new List<Ir6502.Instruction>
            {
                // Set up various flag states
                new Ir6502.Copy(new Ir6502.Constant((byte)(carryState ? 1 : 0)), new Ir6502.Flag(Ir6502.FlagName.Carry)),
                new Ir6502.Copy(new Ir6502.Constant((byte)(zeroState ? 1 : 0)), new Ir6502.Flag(Ir6502.FlagName.Zero)),
                new Ir6502.Copy(new Ir6502.Constant((byte)(negativeState ? 1 : 0)), new Ir6502.Flag(Ir6502.FlagName.Negative)),
                new Ir6502.Copy(new Ir6502.Constant((byte)(overflowState ? 1 : 0)), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

                // Add the JMP instruction
                nesIrInstructions[0],

                // This should always be skipped
                new Ir6502.Copy(new Ir6502.Constant(44), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

                // Target label
                new Ir6502.Label(new Ir6502.Identifier("always_target")),

                // This should always be executed
                new Ir6502.Copy(new Ir6502.Constant(55), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            };

            var jit = new TestJitCompiler();
            jit.AddMethod(0x1234, allInstructions);

            // Set flag states
            jit.TestHal.SetFlag(CpuStatusFlags.Carry, carryState);
            jit.TestHal.SetFlag(CpuStatusFlags.Zero, zeroState);
            jit.TestHal.SetFlag(CpuStatusFlags.Negative, negativeState);
            jit.TestHal.SetFlag(CpuStatusFlags.Overflow, overflowState);

            jit.RunMethod(0x1234);

            // JMP should always jump, regardless of flag states
            jit.TestHal.XRegister.ShouldBe((byte)0,
                $"Jump should always be taken (C:{carryState}, Z:{zeroState}, N:{negativeState}, V:{overflowState})");
            jit.TestHal.ARegister.ShouldBe((byte)55,
                $"Target should always be reached (C:{carryState}, Z:{zeroState}, N:{negativeState}, V:{overflowState})");
        }
    }
}