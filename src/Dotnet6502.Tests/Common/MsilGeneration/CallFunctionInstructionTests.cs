using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class CallFunctionInstructionTests
{
    [Fact]
    public void Can_Call_Single_Function()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function at address 0x2000 that writes a test value to memory
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(0x3000, null, false));
        jit.AddMethod(0x2000, [callableInstruction]);

        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Call_Multiple_Functions_In_Sequence()
    {
        var instructions = new Ir6502.Instruction[]
        {
            new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false)),
            new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2100, false))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, instructions);

        // Add first callable function at address 0x2000 that writes 10 to memory
        var function1Instruction = new Ir6502.Copy(
            new Ir6502.Constant(10),
            new Ir6502.Memory(0x3000, null, false));
        jit.AddMethod(0x2000, [function1Instruction]);

        // Add second callable function at address 0x2100 that writes 20 to memory
        var function2Instruction = new Ir6502.Copy(
            new Ir6502.Constant(20),
            new Ir6502.Memory(0x3001, null, false));
        jit.AddMethod(0x2100, [function2Instruction]);

        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)10);
        jit.TestHal.ReadMemory(0x3001).ShouldBe((byte)20);
    }

    [Fact]
    public void Can_Call_Same_Function_Multiple_Times()
    {
        var instructions = new Ir6502.Instruction[]
        {
            new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false)),
            new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false)),
            new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false))
        };

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, instructions);

        // Add a callable function that increments a counter in memory
        var counterInstructions = new Ir6502.Instruction[]
        {
            new Ir6502.Binary(
                Ir6502.BinaryOperator.Add,
                new Ir6502.Memory(0x3000, null, false),
                new Ir6502.Constant(1),
                new Ir6502.Memory(0x3000, null, false))
        };
        jit.AddMethod(0x2000, counterInstructions);

        // Initialize counter to 0
        jit.TestHal.WriteMemory(0x3000, 0);
        jit.RunMethod(0x1234);

        // Should have been called 3 times
        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)3);
    }

    [Fact]
    public void Throws_Exception_When_Function_Not_Defined()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);

        // Don't add the callable function at 0x9999, so it should throw when trying to call it
        Should.Throw<InvalidOperationException>(() => jit.RunMethod(0x1234))
            .Message.ShouldContain("Method at address 2000 called but no method prepared for that");
    }

    [Fact]
    public void Can_Call_Function_Via_Indirect_Address()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x20AB, true));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function that writes a test value to memory, and point some memory to that address
        jit.MemoryMap.MemoryBlock[0x20AB] = 0x45;
        jit.MemoryMap.MemoryBlock[0x20AC] = 0x23;

        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(0x3000, null, false));
        jit.AddMethod(0x2345, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Call_Function_Via_Indirect_Address_Including_Page_Boundary_Bug()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x20FF, true));

        var jit = new TestJitCompiler();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function that writes a test value to memory, and point some memory to that address.
        // 6502 has a bug that an indirect jump across page boundaries doesn't increment the page number.
        jit.MemoryMap.MemoryBlock[0x20FF] = 0x45;
        jit.MemoryMap.MemoryBlock[0x2000] = 0x23;

        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(0x3000, null, false));
        jit.AddMethod(0x2345, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }
}