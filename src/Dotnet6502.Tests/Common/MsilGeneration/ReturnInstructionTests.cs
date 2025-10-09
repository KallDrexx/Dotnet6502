using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class ReturnInstructionTests
{
    [Fact]
    public void Return_Triggers_Function_Call_To_Provided_Memory_Address()
    {
        var returnAddress = new Ir6502.Variable(0);
        var setAddress = new Ir6502.Copy(new Ir6502.Constant(0x25), returnAddress);

        var setAccumulator = new Ir6502.Copy(
            new Ir6502.Constant(77),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var returnInstruction = new Ir6502.Return(returnAddress);
        var modifyAccumulator = new Ir6502.Copy(
            new Ir6502.Constant(11),
            new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x1234, [setAddress, setAccumulator, returnInstruction, modifyAccumulator]);

        jit.AddMethod(0x0025, [
            new Ir6502.Copy(new Ir6502.Constant(43), new Ir6502.Register(Ir6502.RegisterName.XIndex)),
        ]);

        jit.RunMethod(0x1234);

        jit.TestHal.ARegister.ShouldBe((byte)77);
        jit.TestHal.XRegister.ShouldBe((byte)43);
    }
}