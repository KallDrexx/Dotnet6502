using System.Text.Json;
using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;
using Shouldly;

namespace Dotnet6502.Tests.Comprehensive;

public static class TestCaseRunner
{
    public static async Task Run(string jsonFile)
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
}