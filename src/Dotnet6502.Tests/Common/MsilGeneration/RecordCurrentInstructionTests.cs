using Dotnet6502.Common.Compilation;
using Shouldly;

namespace Dotnet6502.Tests.Common.MsilGeneration;

public class RecordCurrentInstructionTests
{
    [Fact]
    public void Sets_Current_Instruction_Value_To_Correct_Value()
    {
        var instruction = new Ir6502.RecordCurrentInstructionAddress(0x5332);

        var jit = TestJitCompiler.Create();
        jit.AddMethod(0x9898, [instruction]);
        jit.RunMethod(0x9898);

        jit.TestHal.CurrentInstructionAddress.ShouldBe((ushort)0x5332);
    }
}