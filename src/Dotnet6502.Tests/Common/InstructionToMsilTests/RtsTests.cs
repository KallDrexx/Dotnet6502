using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common.InstructionToMsilTests;

public class RtsTests
{
    [Fact]
    public void Rts_Pulls_Return_Address_From_Stack_And_Adds_One()
    {
        var instructionInfo = InstructionSet.GetInstruction(0x60);
        var instruction = new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = [0x60],
        };

        var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
        var allInstructions = InstructionConverter.Convert(instruction, context);

        var jit = TestJitCompiler.Create();
        jit.TestHal.ProcessorStatus = 0;
        jit.TestHal.PushToStack(0x23); // return address low byte
        jit.TestHal.PushToStack(0x45);  // return address high byte

        jit.AddMethod(0x1234, allInstructions);

        // Add a returnable function at the irq address
        var callableInstruction = new Ir6502.Copy(
            new Ir6502.Constant(99),
            new Ir6502.Memory(0x4000, null, false));
        jit.AddMethod(0x2346, [callableInstruction]);

        jit.RunMethod(0x1234);
        jit.TestHal.ReadMemory(0x4000).ShouldBe((byte)99); // Verify return address was called
    }
}