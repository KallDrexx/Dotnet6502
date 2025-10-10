using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class PollForInterruptsTests
{
    [Fact]
    public void Runs_Next_Instruction_When_Zero_Address_Returned()
    {
        var poll = new Ir6502.PollForInterrupt(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.NextInterruptLocation = 0;
        jit.AddMethod(0x1234, [poll, nextOp]);

        jit.RunMethod(0x1234);
        jit.TestHal.ARegister.ShouldBe((byte)12);
    }

    [Fact]
    public void Calls_Method_From_Memory_Address_Without_Running_Next_Instruction_When_Non_Zero_Address_Returned()
    {
        var poll = new Ir6502.PollForInterrupt(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));
        var setX = new Ir6502.Copy(new Ir6502.Constant(56), new Ir6502.Register(Ir6502.RegisterName.XIndex));

        var jit = TestJitCompiler.Create();
        jit.TestHal.NextInterruptLocation = 0xFFFA;
        jit.TestHal.WriteMemory(0xFFFA, 0x78);
        jit.TestHal.WriteMemory(0xFFFB, 0x90);
        jit.AddMethod(0x1234, [poll, nextOp]);
        jit.AddMethod(0x9078, [setX]);

        jit.RunMethod(0x1234);
        jit.TestHal.ARegister.ShouldBe((byte)0);
        jit.TestHal.XRegister.ShouldBe((byte)56);
    }

    [Fact]
    public void Pushes_Passed_In_Address_And_Current_Flags_To_The_Stack()
    {
        var poll = new Ir6502.PollForInterrupt(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.NextInterruptLocation = 0xFFFA;
        jit.TestHal.ProcessorStatus = 0b11001111;
        jit.TestHal.WriteMemory(0xFFFA, 0x78);
        jit.TestHal.WriteMemory(0xFFFB, 0x90);
        jit.AddMethod(0x1234, [poll, nextOp]);
        jit.AddMethod(0x9078, []);

        jit.RunMethod(0x1234);
        jit.TestHal.PopFromStack().ShouldBe((byte)0b11001111);
        jit.TestHal.PopFromStack().ShouldBe((byte)0x56);
        jit.TestHal.PopFromStack().ShouldBe((byte)0x34);
    }

    [Fact]
    public void Sets_Interrupt_Disabled_Flag()
    {
        var poll = new Ir6502.PollForInterrupt(0x3456);
        var nextOp = new Ir6502.Copy(new Ir6502.Constant(12), new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        var jit = TestJitCompiler.Create();
        jit.TestHal.NextInterruptLocation = 0xFFFA;
        jit.TestHal.ProcessorStatus = 0;
        jit.TestHal.WriteMemory(0xFFFA, 0x78);
        jit.TestHal.WriteMemory(0xFFFB, 0x90);
        jit.AddMethod(0x1234, [poll, nextOp]);
        jit.AddMethod(0x9078, []);

        jit.RunMethod(0x1234);
        jit.TestHal.GetFlag(CpuStatusFlags.InterruptDisable).ShouldBeTrue();
    }
}