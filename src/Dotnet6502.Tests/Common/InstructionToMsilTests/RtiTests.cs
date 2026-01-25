using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class RtiTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Rti_Updates_Flag_From_Stack_First_Stack_Value_Return_Address_From_Second_And_Third(bool useInterpreter)
    {
        var instructionInfo = InstructionSet.GetInstruction(0x40);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x40],
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.AlwaysUseInterpreter = useInterpreter;
        jit.TestHal.ProcessorStatus = 0;
        jit.TestHal.PushToStack(0x23); // return address low byte
        jit.TestHal.PushToStack(0x45);  // return address high byte
        jit.TestHal.PushToStack(0b11001111); // status value

        jit.AddMethod(0x1234, allInstructions);

        // Add a returnable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x4000, null, false));
        jit.AddMethod(0x2345, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ProcessorStatus.ShouldBe((byte)0b11101111);
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99); // Verify return address was called
    }
}
