using System.Text;
using NESDecompiler.Core.CPU;

namespace Dotnet6502.Tests.Comprehensive;

public class TestCaseException : Exception
{
    public TestCaseException(
        TestCase testCase,
        TestJitCompiler jitCompiler,
        InstructionInfo instructionInfo,
        Exception innerException) : base(FormMessage(testCase, jitCompiler, instructionInfo), innerException)
    {
    }

    private static string FormMessage(
        TestCase testCase,
        TestJitCompiler jitCompiler,
        InstructionInfo instructionInfo)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Test case '{testCase.Name}' failed:");
        builder.AppendLine($"Parsed Instruction: {instructionInfo.Mnemonic} ({instructionInfo.AddressingMode})");
        builder.AppendLine($"Initial - A:{testCase.Initial.A} " +
                           $"X:{testCase.Initial.X} " +
                           $"Y:{testCase.Initial.Y} " +
                           $"P:{testCase.Initial.P} " +
                           $"S:{testCase.Initial.S}");

        builder.Append("Initial ram: ");
        foreach (var ram in testCase.Initial.Ram)
        {
            builder.Append($"[{ram[0]}, {ram[1]}] ");
        }
        builder.AppendLine();

        var accessedRamAddresses = jitCompiler.MemoryMap.ReadMemoryBlocks
            .Select(x => $"{x} ({x:X4})")
            .Aggregate((x, y) => $"{x}, {y}");

        builder.AppendLine($"Accessed RAM addresses: {accessedRamAddresses}");

        return builder.ToString();
    }
}