using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Tests.Common;

public class TestJitCompiler : IJitCompiler
{
    private readonly Dictionary<ushort, ExecutableMethod> _methods = new();
    public Dictionary<Type, MsilGenerator.CustomIlGenerator>? CustomGenerators { get; set; }

    public TestMemoryMap MemoryMap { get; }
    public Base6502Hal TestHal { get; }

    public TestJitCompiler()
    {
        MemoryMap = new TestMemoryMap();
        TestHal = new TestHal(MemoryMap);
    }

    public void RunMethod(ushort address)
    {
        if (!_methods.TryGetValue(address, out var method))
        {
            var message = $"Method at address {address:X4} called but no method prepared for that";
            throw new InvalidOperationException(message);
        }

        method(this, TestHal);
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
        var method = ExecutableMethodGenerator.Generate($"test_0x{address:X4}", [convertedInstructions], CustomGenerators);
        _methods.Add(address, method);
    }
}