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

    public TestMemoryMap Memory { get; }
    public TestHal TestHal { get; }

    private TestJitCompiler(TestHal testHal, TestMemoryMap memory, MemoryBus memoryBus)
        : base(testHal, null, memoryBus)
    {
        Memory = memory;
        TestHal = testHal;
    }

    public static TestJitCompiler Create()
    {
        var memoryBus = new MemoryBus();
        var memoryMap = new TestMemoryMap();
        memoryBus.Attach(memoryMap, 0);

        var hal = new TestHal(memoryBus);

        return new TestJitCompiler(hal, memoryMap, memoryBus);
    }

    public void AddMethod(ushort address, IReadOnlyList<Ir6502.Instruction> instructions, bool generateDll = false)
    {
        var nop = new DisassembledInstruction
        {
            Info = InstructionSet.GetInstruction(0xEA),
            CPUAddress = address,
            Bytes = [0xEA]
        };

        var function = new DecompiledFunction(address, [nop], new HashSet<ushort>());
        var convertedInstructions = new ConvertedInstruction(nop, instructions);
        var method = ExecutableMethodGenerator.Generate(
            $"test_0x{address:X4}",
            [convertedInstructions],
            CustomGenerators,
            generateDll);

        _executableMethodCache.AddExecutableMethod(method, function);
    }

    protected override IReadOnlyList<ConvertedInstruction> GetIrInstructions(DecompiledFunction function)
    {
        var message = $"Function address 0x{function.Address:X4} called but that address has not been configured";
        throw new InvalidOperationException(message);
    }
}