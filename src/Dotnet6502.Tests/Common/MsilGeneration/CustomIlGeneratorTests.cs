using System.Reflection.Emit;
using Dotnet6502.Common;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class CustomIlGeneratorTests
{
    private record TestInstruction : Ir6502.Instruction;

    [Fact]
    public void Can_Execute_Generator_For_Custom_Instruction()
    {
        void CustomGenerator(Ir6502.Instruction instruction, MsilGenerator.Context context)
        {
            var pushMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.PushToStack))!;
            context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
            context.IlGenerator.Emit(OpCodes.Ldc_I4, 123);
            context.IlGenerator.Emit(OpCodes.Callvirt, pushMethod);
        }

        var customGenerators = new Dictionary<Type, MsilGenerator.CustomIlGenerator>()
        {
            { typeof(TestInstruction), CustomGenerator },
        };

        List<Ir6502.Instruction> instructions =
        [
            new TestInstruction(),
            new Ir6502.PushStackValue(new Ir6502.Constant(55)),
        ];

        var runner = new InstructionTestRunner(instructions, [], customGenerators);
        runner.RunTestMethod();
        
        // First stack value should be the normal stack behavior, while second value should be the 
        // custom one.
        runner.TestHal.PopFromStack().ShouldBe((byte)55);
        runner.TestHal.PopFromStack().ShouldBe((byte)123);
    }
}