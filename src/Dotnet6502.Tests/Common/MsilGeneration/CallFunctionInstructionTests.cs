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

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function at address 0x2000 that writes a test value to memory
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x3000), null, false));
        jit.AddMethod(0x2000, [callableInstruction]);

        jit.RunMethod(0x1234);

        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }

    [Fact]
    public void Throws_Exception_When_Function_Not_Defined()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x2000, false));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);

        // Don't add the callable function at 0x9999, so it should throw when trying to call it
        Should.Throw<InvalidOperationException>(() => jit.RunMethod(0x1234));
    }

    [Fact]
    public void Can_Call_Function_Via_Indirect_Address()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x20AB, true));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function that writes a test value to memory, and point some memory to that address
        jit.Memory.MemoryBlock[0x20AB] = 0x45;
        jit.Memory.MemoryBlock[0x20AC] = 0x23;

        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x3000), null, false));
        jit.AddMethod(0x2345, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }

    [Fact]
    public void Can_Call_Function_Via_Indirect_Address_Including_Page_Boundary_Bug()
    {
        var instruction = new Ir6502.CallFunction(new Ir6502.FunctionAddress(0x20FF, true));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [instruction]);

        // Add a callable function that writes a test value to memory, and point some memory to that address.
        // 6502 has a bug that an indirect jump across page boundaries doesn't increment the page number.
        jit.Memory.MemoryBlock[0x20FF] = 0x45;
        jit.Memory.MemoryBlock[0x2000] = 0x23;

        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(42),
            new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x3000), null, false));
        jit.AddMethod(0x2345, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ReadMemory(0x3000).ShouldBe((byte)42);
    }
}