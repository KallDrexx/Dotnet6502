using System.Reflection.Emit;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class CustomIlGeneratorTests
{
    private record TestInstruction : Ir6502.Instruction;

    [Fact]
    public void Can_Execute_Generator_For_Custom_Instruction()
    {
        static void CustomGenerator(Ir6502.Instruction instruction, ILGenerator ilGenerator)
        {
            var pushMethod = typeof(Base6502Hal<GenericMemoryBus>).GetMethod(nameof(Base6502Hal<GenericMemoryBus>.PushToStack))!;
            ilGenerator.Emit(JitCompiler<Base6502Hal<GenericMemoryBus>>.LoadHalArg);
            ilGenerator.Emit(OpCodes.Ldc_I4, 123);
            ilGenerator.Emit(OpCodes.Callvirt, pushMethod);
        }

        var customGenerators = new Dictionary<Type, MsilGenerator<TestHal>.CustomIlGenerator>()
        {
            { typeof(TestInstruction), CustomGenerator },
        };

        List<Ir6502.Instruction> instructions =
        [
            new TestInstruction(),
            new Ir6502.PushStackValue(new Ir6502.Constant(55)),
        ];

        var jit = TestJitCompiler.Create();
        jit.CustomGenerators = customGenerators;
        jit.AddMethod(0x1234, instructions);
        jit.RunMethod(0x1234);
        
        // First stack value should be the normal stack behavior, while second value should be the 
        // custom one.
        jit.TestHal.PopFromStack().ShouldBe((byte)55);
        jit.TestHal.PopFromStack().ShouldBe((byte)123);
    }
}
