using Dotnet6502.Common;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Tests.Common;

public class TestJitCompiler : IJitCompiler
{
    private readonly Dictionary<ushort, ExecutableMethod> _methods = new();

    public Test6502Hal Hal { get; } = new();

    public void RunMethod(ushort address)
    {
        if (!_methods.TryGetValue(address, out var method))
        {
            var message = $"Method at address {address:X4} called but no method prepared for that";
            throw new InvalidOperationException(message);
        }

        method(this, Hal);
    }

    public void AddMethod(ushort address, IReadOnlyList<Ir6502.Instruction> instructions)
    {
        var nop = new DisassembledInstruction
        {
            Info = InstructionSet.GetInstruction(0xEA),
            CPUAddress = address,
        };

        var convertedInstructions = instructions
            .Select(x => new ConvertedInstruction(nop, instructions))
            .ToArray();

        var method = ExecutableMethodGenerator.Generate($"test_{address}", convertedInstructions);
        _methods.Add(address, method);
    }
}