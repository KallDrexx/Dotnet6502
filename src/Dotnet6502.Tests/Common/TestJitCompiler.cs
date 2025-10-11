using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;

namespace Dotnet6502.Tests.Common;

public class TestJitCompiler : JitCompiler
{
    public Dictionary<Type, MsilGenerator.CustomIlGenerator>? CustomGenerators { get; set; }

    public TestMemoryMap MemoryMap { get; }
    public TestHal TestHal { get; }

    private TestJitCompiler(TestHal testHal, TestMemoryMap memoryMap)
        : base(testHal, null, memoryMap)
    {
        MemoryMap = memoryMap;
        TestHal = testHal;
    }

    public static TestJitCompiler Create()
    {
        var memoryMap = new TestMemoryMap();
        var hal = new TestHal(memoryMap);

        return new TestJitCompiler(hal, memoryMap);
    }

    public void AddMethod(ushort address, IReadOnlyList<Ir6502.Instruction> instructions, bool generateDll = false)
    {
        var nop = new DisassembledInstruction
        {
            Info = InstructionSet.GetInstruction(0xEA),
            CPUAddress = address,
            Bytes = [0xEA]
        };

        var convertedInstructions = new ConvertedInstruction(nop, instructions);
        var method = ExecutableMethodGenerator.Generate(
            $"test_0x{address:X4}",
            [convertedInstructions],
            CustomGenerators,
            generateDll);

        CompiledMethods.Add(address, method);
    }

    protected override IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        var message = $"Function address 0x{address:X4} called but that address has not been configured";
        throw new InvalidOperationException(message);
    }
}