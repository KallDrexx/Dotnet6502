using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.InstructionToMsilTests;

/// <summary>
/// Tests for 6502 JSR (Jump to Subroutine) instruction
///
/// JSR calls a subroutine at the target address:
/// - JSR Absolute (0x20): Call subroutine at 16-bit absolute address
/// - Converts to NesIr.CallFunction instruction
/// - Does NOT affect any flags
/// - Does NOT affect any registers
/// - Requires function to be defined in the InstructionConverter.Context
/// - Target address must be mapped to a known function
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
        var functions = new Dictionary<ushort, Function>
        {
            { 0x9000, new Function(0x9000, "TestFunction") }
        };
        var context = new InstructionConverter.Context(labels, functions);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        // Add setup and verification instructions around the JSR call
        var allInstructions = new List<Ir6502.Instruction>
        {
            // Set up initial state
            new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Register(Ir6502.RegisterName.Accumulator)),

            // Add the JSR instruction (converts to CallFunction)
            nesIrInstructions[0],

            // Instruction executed after function call
            new Ir6502.Copy(new Ir6502.Constant(42), new Ir6502.Register(Ir6502.RegisterName.XIndex))
        };

        var callableFunctions = new[] { "TestFunction" };
        var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);
        testRunner.RunTestMethod();

        // Verify the function was called and execution continued
        testRunner.NesHal.XRegister.ShouldBe((byte)42); // Should be executed after JSR

        // Verify the function was actually invoked
        var (address, expectedValue) = testRunner.GetCallableMethodSignature("TestFunction", callableFunctions);
        testRunner.NesHal.ReadMemory(address).ShouldBe(expectedValue);
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
        var context = new InstructionConverter.Context(labels, functions);

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

        var callableFunctions = new[] { "FirstFunction", "SecondFunction" };
        var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);
        testRunner.RunTestMethod();

        // Verify both functions were called and all instructions executed
        testRunner.NesHal.XRegister.ShouldBe((byte)77);
        testRunner.NesHal.YRegister.ShouldBe((byte)88);

        // Verify both functions were actually invoked
        var (address1, expectedValue1) = testRunner.GetCallableMethodSignature("FirstFunction", callableFunctions);
        var (address2, expectedValue2) = testRunner.GetCallableMethodSignature("SecondFunction", callableFunctions);
        testRunner.NesHal.ReadMemory(address1).ShouldBe(expectedValue1);
        testRunner.NesHal.ReadMemory(address2).ShouldBe(expectedValue2);
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
        var context = new InstructionConverter.Context(labels, functions);

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

        var callableFunctions = new[] { "RepeatedFunction" };
        var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);
        testRunner.RunTestMethod();

        // Verify all instructions executed (function was called each time)
        testRunner.NesHal.XRegister.ShouldBe((byte)11);
        testRunner.NesHal.YRegister.ShouldBe((byte)22);
        testRunner.NesHal.ARegister.ShouldBe((byte)33);

        // Verify the function was actually invoked
        var (address, expectedValue) = testRunner.GetCallableMethodSignature("RepeatedFunction", callableFunctions);
        testRunner.NesHal.ReadMemory(address).ShouldBe(expectedValue);
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
        var context = new InstructionConverter.Context(labels, functions);

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

        var callableFunctions = new[] { "TestFunction" };
        var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);

        // Set initial flag states
        testRunner.NesHal.Flags[CpuStatusFlags.Carry] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Zero] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Negative] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable] = true;
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal] = true;

        testRunner.RunTestMethod();

        // JSR should not affect any flags
        testRunner.NesHal.Flags[CpuStatusFlags.Carry].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Zero].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Negative].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Overflow].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.InterruptDisable].ShouldBeTrue();
        testRunner.NesHal.Flags[CpuStatusFlags.Decimal].ShouldBeTrue();
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
        var context = new InstructionConverter.Context(labels, functions);

        var nesIrInstructions = InstructionConverter.Convert(instruction, context);

        var allInstructions = new List<Ir6502.Instruction>
        {
            // Add the JSR instruction
            nesIrInstructions[0]
        };

        var callableFunctions = new[] { "TestFunction" };
        var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);

        // Set initial register values
        testRunner.NesHal.ARegister = 0x42;
        testRunner.NesHal.XRegister = 0x33;
        testRunner.NesHal.YRegister = 0x77;
        testRunner.NesHal.StackPointer = 0xFF;

        testRunner.RunTestMethod();

        // JSR should not affect any registers
        testRunner.NesHal.ARegister.ShouldBe((byte)0x42);
        testRunner.NesHal.XRegister.ShouldBe((byte)0x33);
        testRunner.NesHal.YRegister.ShouldBe((byte)0x77);
        testRunner.NesHal.StackPointer.ShouldBe((byte)0xFF);
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
            var context = new InstructionConverter.Context(labels, functions);

            var nesIrInstructions = InstructionConverter.Convert(instruction, context);

            var allInstructions = new List<Ir6502.Instruction>
            {
                nesIrInstructions[0],
                new Ir6502.Copy(new Ir6502.Constant(99), new Ir6502.Register(Ir6502.RegisterName.XIndex))
            };

            var callableFunctions = new[] { testCase.Name };
            var testRunner = new InstructionTestRunner(allInstructions, callableFunctions);
            testRunner.RunTestMethod();

            // Verify function was called and execution continued
            testRunner.NesHal.XRegister.ShouldBe((byte)99);

            var (address, expectedValue) = testRunner.GetCallableMethodSignature(testCase.Name, callableFunctions);
            testRunner.NesHal.ReadMemory(address).ShouldBe(expectedValue);
        }
    }

    [Fact]
    public void JSR_Function_Not_Defined_Throws_Exception()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x20);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x20, 0x00, 0x90],
            TargetAddress = 0x9000 // This address will not have a function defined
        };

        var labels = new Dictionary<ushort, string>();
        var functions = new Dictionary<ushort, Function>(); // Empty - no functions defined
        var context = new InstructionConverter.Context(labels, functions);

        // Should throw exception when trying to convert JSR to undefined function
        Should.Throw<InvalidOperationException>(() => InstructionConverter.Convert(instruction, context))
            .Message.ShouldContain("JSR instruction to address '36864' but that address is not tied to a known function");
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
        var context = new InstructionConverter.Context(labels, functions);

        // Should throw exception when JSR has no target address
        Should.Throw<InvalidOperationException>(() => InstructionConverter.Convert(instruction, context))
            .Message.ShouldContain("JSR instruction with no target address");
    }
}