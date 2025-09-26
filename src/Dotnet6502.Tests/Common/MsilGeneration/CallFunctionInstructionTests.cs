using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class CallFunctionInstructionTests
{
    [Fact]
    public void Can_Call_Single_Function()
    {
        var functionName = "TestFunction1";
        var instruction = new Ir6502.CallFunction(new Ir6502.Identifier(functionName));
        var callableFunctions = new[] { functionName };

        var testRunner = new InstructionTestRunner([instruction], callableFunctions);
        testRunner.RunTestMethod();

        var (address, expectedValue) = testRunner.GetCallableMethodSignature(functionName, callableFunctions);
        testRunner.TestHal.ReadMemory(address).ShouldBe(expectedValue);
    }

    [Fact]
    public void Can_Call_Multiple_Functions_In_Sequence()
    {
        var function1 = "FirstFunction";
        var function2 = "SecondFunction";
        var callableFunctions = new[] { function1, function2 };

        var instructions = new Ir6502.Instruction[]
        {
            new Ir6502.CallFunction(new Ir6502.Identifier(function1)),
            new Ir6502.CallFunction(new Ir6502.Identifier(function2))
        };

        var testRunner = new InstructionTestRunner(instructions, callableFunctions);
        testRunner.RunTestMethod();

        var (address1, expectedValue1) = testRunner.GetCallableMethodSignature(function1, callableFunctions);
        var (address2, expectedValue2) = testRunner.GetCallableMethodSignature(function2, callableFunctions);

        testRunner.TestHal.ReadMemory(address1).ShouldBe(expectedValue1);
        testRunner.TestHal.ReadMemory(address2).ShouldBe(expectedValue2);
    }

    [Fact]
    public void Can_Call_Same_Function_Multiple_Times()
    {
        var functionName = "RepeatedFunction";
        var callableFunctions = new[] { functionName };

        var instructions = new Ir6502.Instruction[]
        {
            new Ir6502.CallFunction(new Ir6502.Identifier(functionName)),
            new Ir6502.CallFunction(new Ir6502.Identifier(functionName)),
            new Ir6502.CallFunction(new Ir6502.Identifier(functionName))
        };

        var testRunner = new InstructionTestRunner(instructions, callableFunctions);
        testRunner.RunTestMethod();

        var (address, expectedValue) = testRunner.GetCallableMethodSignature(functionName, callableFunctions);
        testRunner.TestHal.ReadMemory(address).ShouldBe(expectedValue);
    }

    [Fact]
    public void Throws_Exception_When_Function_Not_Defined()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.Identifier("NonExistentFunction"));
        var callableFunctions = new[] { "DifferentFunction" };

        Should.Throw<InvalidOperationException>(() => new InstructionTestRunner([instruction], callableFunctions))
            .Message.ShouldContain("No known method with the name 'NonExistentFunction' exists");
    }
}