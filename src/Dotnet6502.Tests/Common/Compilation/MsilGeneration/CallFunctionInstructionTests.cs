using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.Compilation.MsilGeneration;

public class CallFunctionInstructionTests
{
    [Fact]
    public void Can_Call_Single_Function()
    {
        var functionName = "TestFunction1";
        var instruction = new NesIr.CallFunction(new NesIr.Identifier(functionName));
        var callableFunctions = new[] { functionName };

        var testRunner = new InstructionTestRunner([instruction], callableFunctions);
        testRunner.RunTestMethod();

        var (address, expectedValue) = testRunner.GetCallableMethodSignature(functionName, callableFunctions);
        testRunner.NesHal.ReadMemory(address).ShouldBe(expectedValue);
    }

    [Fact]
    public void Can_Call_Multiple_Functions_In_Sequence()
    {
        var function1 = "FirstFunction";
        var function2 = "SecondFunction";
        var callableFunctions = new[] { function1, function2 };

        var instructions = new NesIr.Instruction[]
        {
            new NesIr.CallFunction(new NesIr.Identifier(function1)),
            new NesIr.CallFunction(new NesIr.Identifier(function2))
        };

        var testRunner = new InstructionTestRunner(instructions, callableFunctions);
        testRunner.RunTestMethod();

        var (address1, expectedValue1) = testRunner.GetCallableMethodSignature(function1, callableFunctions);
        var (address2, expectedValue2) = testRunner.GetCallableMethodSignature(function2, callableFunctions);

        testRunner.NesHal.ReadMemory(address1).ShouldBe(expectedValue1);
        testRunner.NesHal.ReadMemory(address2).ShouldBe(expectedValue2);
    }

    [Fact]
    public void Can_Call_Same_Function_Multiple_Times()
    {
        var functionName = "RepeatedFunction";
        var callableFunctions = new[] { functionName };

        var instructions = new NesIr.Instruction[]
        {
            new NesIr.CallFunction(new NesIr.Identifier(functionName)),
            new NesIr.CallFunction(new NesIr.Identifier(functionName)),
            new NesIr.CallFunction(new NesIr.Identifier(functionName))
        };

        var testRunner = new InstructionTestRunner(instructions, callableFunctions);
        testRunner.RunTestMethod();

        var (address, expectedValue) = testRunner.GetCallableMethodSignature(functionName, callableFunctions);
        testRunner.NesHal.ReadMemory(address).ShouldBe(expectedValue);
    }

    [Fact]
    public void Throws_Exception_When_Function_Not_Defined()
    {
        var instruction = new NesIr.CallFunction(new NesIr.Identifier("NonExistentFunction"));
        var callableFunctions = new[] { "DifferentFunction" };

        Should.Throw<InvalidOperationException>(() => new InstructionTestRunner([instruction], callableFunctions))
            .Message.ShouldContain("No known method with the name 'NonExistentFunction' exists");
    }
}