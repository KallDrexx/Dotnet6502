using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 JSR (Jump to Subroutine) instruction
/// </summary>
public class JsrTests
{
    [Fact]
    public void JSR_Basic_Function_Call()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90], // JSR $9000
            TargetAddress = 0x9000 // Target address for function call
        };

        var labels = new Dictionary<ushort, string>();
        var context = new InstructionConverter.Context(labels);

        var allInstructions = InstructionConverter.Convert(instruction, context)
            .Prepend(
                // Set up initial state
                new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            )
            .ToArray();

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address that writes a test value to memory
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x4000, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address from instruction bytes

        jit.RunMethod(0x1234);

        // Verify the function was actually invoked
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99);
    }

    [Fact]
    public void JSR_Does_Not_Affect_Flags()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90],
            TargetAddress = 0x9000
        };

        var labels = new Dictionary<ushort, string>();
        var context = new InstructionConverter.Context(labels);

        var irInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the JSR instruction
            irInstructions[0]
        };

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(102),
            new Ir6502.Memory(0x5003, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address

        // Set initial flag states
        jit.TestHal.SetFlag(CpuStatusFlags.Carry, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Zero, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Negative, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Overflow, true);
        jit.TestHal.SetFlag(CpuStatusFlags.InterruptDisable, true);
        jit.TestHal.SetFlag(CpuStatusFlags.Decimal, true);

        jit.RunMethod(0x1234);

        // JSR should not affect any flags
        jit.TestHal.GetFlag(CpuStatusFlags.Carry).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Zero).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Negative).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Overflow).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
        jit.TestHal.GetFlag(CpuStatusFlags.Decimal).ShouldBeTrue();
    }

    [Fact]
    public void JSR_Does_Not_Affect_Registers()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90],
            TargetAddress = 0x9000,
            CPUAddress = 0x2345,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var irInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, irInstructions);

        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(103),
            new Ir6502.Memory(0x5004, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address

        // Set initial register values
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;

        jit.RunMethod(0x1234);

        // JSR should not affect any registers
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
    }

    [Fact]
    public void JSR_Target_Address_Required()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90],
            TargetAddress = null // No target address specified
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());

        // Should throw exception when JSR has no target address
        Should.Throw<InvalidOperationException>(() => InstructionConverter.Convert(instruction, context))
            .Message.ShouldContain("JSR instruction with no target address");
    }

    [Fact]
    public void JSR_Pushes_Address_Plus_Two_To_The_stack()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90], // JSR $9000
            TargetAddress = 0x9000, // Target address for function call
            CPUAddress = 0x3456,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var allInstructions = InstructionConverter.Convert(instruction, context)
            .Prepend(
                // Set up initial state
                new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
            )
            .ToArray();

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address that writes a test value to memory
        var lowVariable = new Ir6502.Variable(0);
        var highVariable = new Ir6502.Variable(1);
        Ir6502.Instruction[] targetInstructions =
        [
            new Ir6502.PopStackValue(lowVariable),
            new Ir6502.PopStackValue(highVariable),
            new Ir6502.Copy(lowVariable, new Ir6502.Register(Ir6502.RegisterName.XIndex)),
            new Ir6502.Copy(highVariable, new Ir6502.Register(Ir6502.RegisterName.YIndex)),
        ];

        jit.AddMethod(0x9000, targetInstructions);
        jit.RunMethod(0x1234);

        // Verify the function was actually invoked and the values are expected
        jit.TestHal.YRegister.ShouldBe((byte)0x34);
        jit.TestHal.XRegister.ShouldBe((byte)0x58);
    }
}