using System.Text;
using System.Text.Json;
using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Common;

/// <summary>
/// Executes tests using the single step 65x02 tests
/// </summary>
public class CpuJsonTests
{
    [Theory]
    [InlineData("a1.json")]
    [InlineData("a5.json")]
    [InlineData("a9.json")]
    [InlineData("ad.json")]
    [InlineData("b1.json")]
    [InlineData("b5.json")]
    [InlineData("b9.json")]
    [InlineData("bd.json")]
    public async Task Can_Execute_Test_Cases(string jsonFile)
    {
        var jsonFilePath = Path.Combine(Environment.CurrentDirectory, "6502", "v1", jsonFile);
        var content = await File.ReadAllTextAsync(jsonFilePath);
        var testCases = JsonSerializer.Deserialize<TestCase[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        foreach (var testCase in testCases)
        {
            var jit = new TestJitCompiler
            {
                TestHal =
                {
                    ARegister = testCase.Initial.A,
                    XRegister = testCase.Initial.X,
                    YRegister = testCase.Initial.Y,
                    ProcessorStatus = testCase.Initial.P,
                    StackPointer = testCase.Initial.S
                }
            };

            foreach (var ramArrayToSet in testCase.Initial.Ram)
            {
                jit.MemoryMap.MemoryBlock[ramArrayToSet[0]] = (byte)ramArrayToSet[1];
            }

            var bytes = testCase.Name.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
            var instructionInfo = InstructionSet.GetInstruction(bytes[0]);
            var disassembledInstruction = new DisassembledInstruction
            {
                Info = instructionInfo,
                Bytes = bytes,
            };

            try
            {
                var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
                var irInstructions = InstructionConverter.Convert(disassembledInstruction, context);

                jit.AddMethod(testCase.Initial.Pc, irInstructions);
                jit.RunMethod(testCase.Initial.Pc);

                jit.TestHal.ARegister.ShouldBe(testCase.Final.A);
                jit.TestHal.XRegister.ShouldBe(testCase.Final.X);
                jit.TestHal.YRegister.ShouldBe(testCase.Final.Y);
                jit.TestHal.ProcessorStatus.ShouldBe(testCase.Final.P);
                jit.TestHal.StackPointer.ShouldBe(testCase.Final.S);

                foreach (var ram in testCase.Final.Ram)
                {
                    var location = ram[0];
                    var value = ram[1];
                    jit.MemoryMap.MemoryBlock[location].ShouldBe((byte)value);
                }
            }
            catch (Exception exception)
            {
                throw new TestCaseException(testCase, jit, instructionInfo, exception);
            }
        }
    }

    private class TestCaseException : Exception
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

    private class TestCase
    {
        public required string Name { get; set; }
        public required ValueSet Initial { get; set; }
        public required ValueSet Final { get; set; }

        public class ValueSet
        {
            public ushort Pc { get; set; }
            public byte S { get; set; }
            public byte A { get; set; }
            public byte X { get; set; }
            public byte Y { get; set; }
            public byte P { get; set; }
            public ushort[][] Ram { get; set; } = [];
        }
    }
}