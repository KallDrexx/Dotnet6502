using System.Text.Json;
using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.ComprehensiveTestRunner;

public static class TestRunner
{
    public static async Task<(int totalTests, List<TestFailure> failures)> RunTestsForMnemonic(string mnemonic)
    {
        var failures = new List<TestFailure>();
        var totalTests = 0;

        // Find all opcodes that match this mnemonic
        var matchingOpcodes = new List<byte>();
        for (byte opcode = 0; opcode <= 0xFF; opcode++)
        {
            var instructionInfo = InstructionSet.GetInstruction(opcode);
            if (instructionInfo.Mnemonic.Equals(mnemonic, StringComparison.OrdinalIgnoreCase))
            {
                matchingOpcodes.Add(opcode);
            }

            if (opcode == 0xFF) break; // Prevent overflow
        }

        if (matchingOpcodes.Count == 0)
        {
            Console.WriteLine($"No opcodes found for mnemonic: {mnemonic}");
            return (0, failures);
        }

        Console.WriteLine($"Found {matchingOpcodes.Count} opcode(s) for mnemonic {mnemonic}:");
        foreach (var opcode in matchingOpcodes)
        {
            Console.WriteLine($"  0x{opcode:X2}");
        }
        Console.WriteLine();

        // Run tests for each matching opcode
        foreach (var opcode in matchingOpcodes)
        {
            var jsonFile = $"{opcode:x2}.json";
            var jsonFilePath = Path.Combine(Environment.CurrentDirectory, "6502", "v1", jsonFile);

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"Warning: Test file not found: {jsonFilePath}");
                continue;
            }

            Console.WriteLine($"Running tests for opcode 0x{opcode:X2} from {jsonFile}...");
            var startTime = DateTime.Now;
            var (testCount, opcodeFailures) = await RunTestsForOpcode(opcode, jsonFilePath, mnemonic);
            var elapsed = DateTime.Now - startTime;
            totalTests += testCount;
            failures.AddRange(opcodeFailures);
            Console.WriteLine($"  {testCount} test(s), {opcodeFailures.Count} failure(s), {elapsed.TotalSeconds:F2}s");
        }

        return (totalTests, failures);
    }

    private static async Task<(int testCount, List<TestFailure> failures)> RunTestsForOpcode(
        byte opcode, string jsonFilePath, string mnemonic)
    {
        var failures = new List<TestFailure>();

        var content = await File.ReadAllTextAsync(jsonFilePath);
        var testCases = JsonSerializer.Deserialize<TestCase[]>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (testCases == null || testCases.Length == 0)
        {
            return (0, failures);
        }

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

                // Check all expected values
                var hasFailure = false;
                var errorMessages = new List<string>();

                if (jit.TestHal.ARegister != testCase.Final.A)
                {
                    hasFailure = true;
                    errorMessages.Add($"A: expected {testCase.Final.A}, actual {jit.TestHal.ARegister}");
                }

                if (jit.TestHal.XRegister != testCase.Final.X)
                {
                    hasFailure = true;
                    errorMessages.Add($"X: expected {testCase.Final.X}, actual {jit.TestHal.XRegister}");
                }

                if (jit.TestHal.YRegister != testCase.Final.Y)
                {
                    hasFailure = true;
                    errorMessages.Add($"Y: expected {testCase.Final.Y}, actual {jit.TestHal.YRegister}");
                }

                if (jit.TestHal.ProcessorStatus != testCase.Final.P)
                {
                    hasFailure = true;
                    errorMessages.Add($"P: expected {testCase.Final.P}, actual {jit.TestHal.ProcessorStatus}");
                }

                if (jit.TestHal.StackPointer != testCase.Final.S)
                {
                    hasFailure = true;
                    errorMessages.Add($"S: expected {testCase.Final.S}, actual {jit.TestHal.StackPointer}");
                }

                // Check RAM values
                foreach (var ram in testCase.Final.Ram)
                {
                    var location = ram[0];
                    var expectedValue = (byte)ram[1];
                    var actualValue = jit.MemoryMap.MemoryBlock[location];
                    if (actualValue != expectedValue)
                    {
                        hasFailure = true;
                        errorMessages.Add($"RAM[{location}]: expected {expectedValue}, actual {actualValue}");
                    }
                }

                if (hasFailure)
                {
                    failures.Add(CreateTestFailure(testCase, jit, mnemonic, string.Join("; ", errorMessages)));
                }
            }
            catch (Exception exception)
            {
                failures.Add(CreateTestFailure(testCase, jit, mnemonic, exception.Message));
            }
        }

        return (testCases.Length, failures);
    }

    private static TestFailure CreateTestFailure(
        TestCase testCase, TestJitCompiler jit, string mnemonic, string errorMessage)
    {
        var initialRam = FormatRam(testCase.Initial.Ram);
        var expectedRam = FormatRam(testCase.Final.Ram);
        var actualRam = FormatActualRam(testCase.Final.Ram, jit.MemoryMap.MemoryBlock);
        var readRamAddresses = jit.MemoryMap.ReadMemoryBlocks.Any()
            ? string.Join(", ", jit.MemoryMap.ReadMemoryBlocks.Select(x => $"{x}(0x{x:X4})"))
            : "<None>";

        return new TestFailure
        {
            Mnemonic = mnemonic,
            HexBytes = testCase.Name,
            InitialA = testCase.Initial.A,
            InitialX = testCase.Initial.X,
            InitialY = testCase.Initial.Y,
            InitialP = testCase.Initial.P,
            InitialS = testCase.Initial.S,
            InitialPc = testCase.Initial.Pc,
            InitialRam = initialRam,
            ExpectedA = testCase.Final.A,
            ExpectedX = testCase.Final.X,
            ExpectedY = testCase.Final.Y,
            ExpectedP = testCase.Final.P,
            ExpectedS = testCase.Final.S,
            ExpectedPc = testCase.Final.Pc,
            ExpectedRam = expectedRam,
            ActualA = jit.TestHal.ARegister,
            ActualX = jit.TestHal.XRegister,
            ActualY = jit.TestHal.YRegister,
            ActualP = jit.TestHal.ProcessorStatus,
            ActualS = jit.TestHal.StackPointer,
            ActualPc = testCase.Final.Pc, // PC is not checked/tracked by the test harness
            ActualRam = actualRam,
            ReadRamAddresses = readRamAddresses,
            ErrorMessage = errorMessage
        };
    }

    private static string FormatRam(ushort[][] ram)
    {
        if (ram.Length == 0) return "<None>";
        return string.Join(", ", ram.Select(r => $"[{r[0]}]={r[1]} (0x{r[1]:X2})"));
    }

    private static string FormatActualRam(ushort[][] expectedRam, byte[] memoryBlock)
    {
        if (expectedRam.Length == 0) return "<None>";
        return string.Join(", ", expectedRam.Select(r => $"[{r[0]}]={memoryBlock[r[0]]}"));
    }
}
