using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common;

public class JitCompilerTests
{
    [Fact]
    public void Patch_Executes_For_Specified_Address()
    {
        List<Ir6502.Instruction> irInstructions = [
            new Ir6502.Copy(
                new Ir6502.Constant(40),
                new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        ];

        var patch = new TestPatch
        {
            FallThrough = false,
            NextAddress = 0x0001,
        };

        var jit = TestJitCompiler.Create();
        jit.AddPatch(patch);
        jit.AddMethod(patch.FunctionEntryAddress, irInstructions);
        jit.AddMethod(patch.NextAddress, [new Ir6502.NoOp()]);
        jit.RunMethod(patch.FunctionEntryAddress);

        jit.TestHal.ARegister.ShouldBe((byte)23);
    }

    [Fact]
    public void Patch_Can_Fall_Through()
    {
        List<Ir6502.Instruction> irInstructions = [
            new Ir6502.Copy(
                new Ir6502.Constant(40),
                new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        ];

        var patch = new TestPatch
        {
            FallThrough = true,
            NextAddress = 0x0001,
        };

        var jit = TestJitCompiler.Create();
        jit.AddPatch(patch);
        jit.AddMethod(patch.FunctionEntryAddress, irInstructions);
        jit.RunMethod(patch.FunctionEntryAddress);

        jit.TestHal.ARegister.ShouldBe((byte)40);
    }

    [Fact]
    public void Patch_Doesnt_Apply_To_Other_Addresses()
    {
        List<Ir6502.Instruction> irInstructions = [
            new Ir6502.Copy(
                new Ir6502.Constant(40),
                new Ir6502.Register(Ir6502.RegisterName.Accumulator))
        ];

        var patch = new TestPatch
        {
            FallThrough = false,
            NextAddress = 0x0001,
        };

        var jit = TestJitCompiler.Create();
        jit.AddPatch(patch);
        jit.AddMethod((ushort)(patch.FunctionEntryAddress + 1), irInstructions);
        jit.RunMethod((ushort)(patch.FunctionEntryAddress + 1));

        jit.TestHal.ARegister.ShouldBe((byte)40);
    }

    private class TestPatch : Patch
    {
        public override ushort FunctionEntryAddress => 0x1234;

        public bool FallThrough { get; set; }
        public ushort NextAddress { get; set; }

        protected override int NativeFunction(Base6502Hal hal)
        {
            if (FallThrough)
            {
                return -1;
            }

            hal.ARegister = 23;

            return NextAddress;
        }
    }
}