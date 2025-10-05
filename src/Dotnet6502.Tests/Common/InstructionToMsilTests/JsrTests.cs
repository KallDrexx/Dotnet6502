using Dotnet6502.Common;
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
            .Append(
                // Instruction executed after function call
                new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.XIndex))
            )
            .ToArray();

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address that writes a test value to memory
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x4000, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address from instruction bytes

        jit.RunMethod(0x1234);

        // Verify the function was called and execution continued
        jit.TestHal.XRegister.ShouldBe((byte)42); // Should be executed after JSR

        // Verify the function was actually invoked
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99);
    }

    [Fact]
    public void JSR_Multiple_Function_Calls()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);

        // First JSR instruction
        var instruction1 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90], // JSR $9000
            TargetAddress = 0x9000
        };

        // Second JSR instruction
        var instruction2 = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x91], // JSR $9100
            TargetAddress = 0x9100
        };

        var labels = new Dictionary<ushort, string>();
        var functions = new Dictionary<ushort, Function>
        {
            { 0x9000, new Function(0x9000, "FirstFunction") },
            { 0x9100, new Function(0x9100, "SecondFunction") }
        };
        var context = new InstructionConverter.Context(labels);

        var nesIrInstructions1 = InstructionConverter.Convert(instruction1, context);
        var nesIrInstructions2 = InstructionConverter.Convert(instruction2, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // First JSR call
            nesIrInstructions1[0],

            // Instruction between calls
            new Ir6502.Copy(new Ir6502.Constant(77), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Second JSR call
            nesIrInstructions2[0],

            // Final instruction
            new Ir6502.Copy(new Ir6502.Constant(88), new Ir6502.Register(Ir6502.RegisterName.YIndex))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Add callable functions at the JSR target addresses
        var callableInstruction1 = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x5000, null, false));
        jit.AddMethod(0x9000, [callableInstruction1]); // First JSR target address

        var callableInstruction2 = new Ir6502.Copy(
            new Ir6502.Constant(100),
            new Ir6502.Memory(0x5001, null, false));
        jit.AddMethod(0x9100, [callableInstruction2]); // Second JSR target address

        jit.RunMethod(0x1234);

        // Verify both functions were called and all instructions executed
        jit.TestHal.XRegister.ShouldBe((byte)77);
        jit.TestHal.YRegister.ShouldBe((byte)88);

        // Verify both functions were actually invoked
        jit.TestHal.ReadMemory(0x5000).ShouldBe((byte)99);
        jit.TestHal.ReadMemory(0x5001).ShouldBe((byte)100);
    }

    [Fact]
    public void JSR_Same_Function_Multiple_Times()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90], // JSR $9000
            TargetAddress = 0x9000
        };

        var labels = new Dictionary<ushort, string>();
        var functions = new Dictionary<ushort, Function>
        {
            { 0x9000, new Function(0x9000, "RepeatedFunction") }
        };
        var context = new InstructionConverter.Context(labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // First call
            nesIrInstructions[0],
            new Ir6502.Copy(new Ir6502.Constant(11), new Ir6502.Register(Ir6502.RegisterName.XIndex)),

            // Second call (create new instruction conversion for same function)
            InstructionConverter.Convert(instruction, context)[0],
            new Ir6502.Copy(new Ir6502.Constant(22), new Ir6502.Register(Ir6502.RegisterName.YIndex)),

            // Third call
            InstructionConverter.Convert(instruction, context)[0],
            new Ir6502.Copy(new Ir6502.Constant(33), new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(101),
            new Ir6502.Memory(0x5002, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address

        jit.RunMethod(0x1234);

        // Verify all instructions executed (function was called each time)
        jit.TestHal.XRegister.ShouldBe((byte)11);
        jit.TestHal.YRegister.ShouldBe((byte)22);
        jit.TestHal.ARegister.ShouldBe((byte)33);

        // Verify the function was actually invoked
        jit.TestHal.ReadMemory(0x5002).ShouldBe((byte)101);
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
        var functions = new Dictionary<ushort, Function>
        {
            { 0x9000, new Function(0x9000, "TestFunction") }
        };
        var context = new InstructionConverter.Context(labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set all flags to known state
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Zero)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Negative)),
            new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Overflow)),

            // Add the JSR instruction
            nesIrInstructions[0]
        };

        var jit = new TestJitCompiler();
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
            TargetAddress = 0x9000
        };

        var labels = new Dictionary<ushort, string>();
        var functions = new Dictionary<ushort, Function>
        {
            { 0x9000, new Function(0x9000, "TestFunction") }
        };
        var context = new InstructionConverter.Context(labels);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JSR instruction
            nesIrInstructions[0]
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Add a callable function at the JSR target address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(103),
            new Ir6502.Memory(0x5004, null, false));
        jit.AddMethod(0x9000, [callableInstruction]); // JSR target address

        // Set initial register values
        jit.TestHal.ARegister = 0x42;
        jit.TestHal.XRegister = 0x33;
        jit.TestHal.YRegister = 0x77;
        jit.TestHal.StackPointer = 0xFF;

        jit.RunMethod(0x1234);

        // JSR should not affect any registers
        jit.TestHal.ARegister.ShouldBe((byte)0x42);
        jit.TestHal.XRegister.ShouldBe((byte)0x33);
        jit.TestHal.YRegister.ShouldBe((byte)0x77);
        jit.TestHal.StackPointer.ShouldBe((byte)0xFF);
    }

    [Fact]
    public void JSR_With_Various_Function_Names()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);

        // Test different function name formats
        var testCases = new[]
        {
            (Address: (ushort)0x9000, Name: "Function1"),
            (Address: (ushort)0x9100, Name: "my_function_2"),
            (Address: (ushort)0x9200, Name: "CamelCaseFunction"),
            (Address: (ushort)0x9300, Name: "UPPER_CASE_FUNC")
        };

        foreach (var testCase in testCases)
        {
            var instruction = new DisassembledInstruction
            {
                Info = instructionInfo,
                Bytes = [0x20, (byte)(testCase.Address & 0xFF), (byte)(testCase.Address >> 8)],
                TargetAddress = testCase.Address
            };

            var labels = new Dictionary<ushort, string>();
            var functions = new Dictionary<ushort, Function>
            {
                { testCase.Address, new Function(testCase.Address, testCase.Name) }
            };
            var context = new InstructionConverter.Context(labels);

            var nesIrInstructions = InstructionConverter.Convert(instruction, context);

            var allInstructions = new List<Ir6502.Instruction>
            {
                nesIrInstructions[0],
                new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex))
            };

            var jit = new TestJitCompiler();
            jit.AddMethod(0x1234, allInstructions);

            // Add a callable function at the JSR target address
            var callableInstruction = new Ir6502.Copy(
                new Ir6502.Constant(104),
                new Ir6502.Memory((ushort)(0x5005 + (testCase.Address - 0x9000) / 0x100), null, false));
            jit.AddMethod(testCase.Address, [callableInstruction]); // JSR target address

            jit.RunMethod(0x1234);

            // Verify function was called and execution continued
            jit.TestHal.XRegister.ShouldBe((byte)99);

            jit.TestHal.ReadMemory((ushort)(0x5005 + (testCase.Address - 0x9000) / 0x100)).ShouldBe((byte)104);
        }
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

        var labels = new Dictionary<ushort, string>();
        var functions = new Dictionary<ushort, Function>();
        var context = new InstructionConverter.Context(labels);

        // Should throw exception when JSR has no target address
        Should.Throw<InvalidOperationException>(() => InstructionConverter.Convert(instruction, context))
            .Message.ShouldContain("JSR instruction with no target address");
    }

    [Fact]
    public void JSR_Calls_Second_Function_If_Different_Address_On_Stack_When_Returned()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90], // JSR $9000
            TargetAddress = 0x9000, // Target address for function call
            CPUAddress = 0x5678,
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var allInstructions = InstructionConverter.Convert(instruction, context)
            .Append(
                // Instruction executed after function call
                new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.XIndex))
            )
            .ToArray();

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, allInstructions);

        // Add the directly called method
        jit.AddMethod(0x9000, [
            new Ir6502.Copy(new Ir6502.Constant(25), new Ir6502.Register(Ir6502.RegisterName.YIndex)),
            new Ir6502.PushStackValue(new Ir6502.Constant(0x98)),
            new Ir6502.PushStackValue(new Ir6502.Constant(0x76)),
        ]);

        // Add equivalent of RTS redirected function
        jit.AddMethod(0x9876, [new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x4000, null, false))
        ]);

        jit.RunMethod(0x1234);

        // Verify the function was called and execution continued
        jit.TestHal.XRegister.ShouldBe((byte)42); // Should be executed after JSR

        // verify the direct function was invoked
        jit.TestHal.YRegister.ShouldBe((byte)25);

        // Verify the indirect function was actually invoked
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99);

    }
}