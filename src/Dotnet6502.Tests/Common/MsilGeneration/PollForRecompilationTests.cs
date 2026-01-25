using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class PollForRecompilationTests
{
    [Fact]
    public void Runs_Next_Instruction_When_Recompilation_Not_Needed()
    {
        var write = new Ir6502.Copy(new Ir6502.Constant(34), new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x1111), null, false));
        var poll = new Ir6502.PollForRecompilation(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.OnMemoryWritten += _ => false;
        jit.AddMethod(0x1234, [write, poll, nextOp]);

        jit.RunMethod(0x1234);
        jit.TestHal.ARegister.ShouldBe((byte)12);
    }

    [Fact]
    public void Calls_Recompilation_Address_When_Recompilation_Requested()
    {
        var write = new Ir6502.Copy(new Ir6502.Constant(34), new Ir6502.Memory(new Ir6502.DirectMemoryLocation(0x1111), null, false));
        var poll = new Ir6502.PollForRecompilation(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var setX = new Ir6502.Copy(new Ir6502.Constant(56), new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.OnMemoryWritten += _ => true;
        jit.AddMethod(0x1234, [write, poll, nextOp]);
        jit.AddMethod(0x3456, [setX]);

        jit.RunMethod(0x1234);
        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)56);
    }
}