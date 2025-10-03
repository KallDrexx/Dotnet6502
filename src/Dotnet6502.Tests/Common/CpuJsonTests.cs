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
    [InlineData("a9.json")]
    public async Task Can_Execute_Test_Cases(string jsonFile)
    {
        var location = Path.Combine(Environment.CurrentDirectory, "6502", "v1", jsonFile);
        var content = await File.ReadAllTextAsync(location);
        var testCases = JsonSerializer.Deserialize<TestCase[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        foreach (var testCase in testCases)
        {
            try
            {
                var memory = new TestMemoryMap();
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
                    memory.MemoryBlock[ramArrayToSet[0]] = (byte)ramArrayToSet[1];
                }

                var bytes = testCase.Name.Split(' ').Select(x => Convert.ToByte(x, 16)).ToArray();
                var instructionInfo = InstructionSet.GetInstruction(bytes[0]);
                var disassembledInstruction = new DisassembledInstruction
                {
                    Info = instructionInfo,
                    Bytes = bytes,
                };

                var context = new InstructionConverter.Context(new Dictionary<ushort, string>());
                var irInstructions = InstructionConverter.Convert(disassembledInstruction, context);

                jit.AddMethod(testCase.Initial.Pc, irInstructions);
                jit.RunMethod(testCase.Initial.Pc);

                jit.TestHal.ARegister.ShouldBe(testCase.Final.A);
                jit.TestHal.XRegister.ShouldBe(testCase.Final.X);
                jit.TestHal.YRegister.ShouldBe(testCase.Final.Y);
                jit.TestHal.ProcessorStatus.ShouldBe(testCase.Final.P);
                jit.TestHal.StackPointer.ShouldBe(testCase.Final.S);
            }
            catch (Exception exception)
            {
                throw new Exception($"Test case '{testCase.Name}' failed", exception);
            }
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