using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;

namespace Dotnet6502.ComprehensiveTestRunner;

public class TestJitCompiler : JitCompiler
{
    public TestMemoryMap MemoryMap { get; }
    public Base6502Hal TestHal { get; }

    private TestJitCompiler(Base6502Hal testHal, TestMemoryMap testMemoryMap, MemoryBus memoryBus)
        : base(testHal, null, memoryBus)
    {
        MemoryMap = testMemoryMap;
        TestHal = testHal;
    }

    public static TestJitCompiler Create()
    {
        var memoryMap = new TestMemoryMap();
        var memoryBus = new MemoryBus();
        memoryBus.Attach(memoryMap, 0);

        var hal = new Base6502Hal(memoryBus);

        return new TestJitCompiler(hal, memoryMap, memoryBus);
    }

    public void AddMethod(ushort address, IReadOnlyList<Ir6502.Instruction> instructions)
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
            new Dictionary<Type, MsilGenerator.CustomIlGenerator>());

        CompiledMethods.Add(address, method);
    }

    protected override IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        var message = $"Function address 0x{address:X4} called but that address has not been configured";
        throw new InvalidOperationException(message);
    }
}
