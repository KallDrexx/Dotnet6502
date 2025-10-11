using System.Reflection.Emit;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler
{
    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_0;

    private readonly Base6502Hal _hal;
    private readonly IReadOnlyList<IJitCustomizer> _jitCustomizers;
    private readonly IMemoryMap _memoryMap;
    protected Dictionary<ushort, ExecutableMethod> CompiledMethods { get; } = new();

    public JitCompiler(Base6502Hal hal, IJitCustomizer? jitCustomizer, IMemoryMap memoryMap)
    {
        _hal = hal;
        _jitCustomizers = jitCustomizer != null
            ? [new StandardJitCustomizer(), jitCustomizer]
            : [new StandardJitCustomizer()];

        _memoryMap = memoryMap;
    }

    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    public void RunMethod(ushort address)
    {
        int nextAddress = address;
        while (nextAddress >= 0 )
        {
            if (!CompiledMethods.TryGetValue((ushort)nextAddress, out var method))
            {
                var instructions = GetIrInstructions((ushort)nextAddress);
                var customGenerators = _jitCustomizers.SelectMany(x => x.GetCustomIlGenerators())
                    .ToDictionary(x => x.Key, x => x.Value);

                method = ExecutableMethodGenerator.Generate($"func_{(ushort)nextAddress:X4}", instructions, customGenerators);
                CompiledMethods.Add((ushort)nextAddress, method);
            }

            _hal.DebugHook($"Entering function 0x{nextAddress:X4}");
            var currentAddress = nextAddress;
            nextAddress = method(_hal);
            _hal.DebugHook($"Exiting function 0x{currentAddress:X4}");
        }
    }

    protected virtual IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        var function = FunctionDecompiler.Decompile(address, _memoryMap.GetCodeRegions());

        if (function.OrderedInstructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} contained no instructions";
            throw new InvalidOperationException(message);
        }

        var instructionConverterContext = new InstructionConverter.Context(function.JumpTargets);

        // Convert each 6502 instruction into one or more IR instructions
        IReadOnlyList<ConvertedInstruction> convertedInstructions = function.OrderedInstructions
            .Select(x => new ConvertedInstruction(x, InstructionConverter.Convert(x, instructionConverterContext)))
            .ToArray();

        // Mutate the instructions based on the JIT customizations being requested
        foreach (var jitCustomizer in _jitCustomizers)
        {
            convertedInstructions = jitCustomizer.MutateInstructions(convertedInstructions);
        }

        if (convertedInstructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} has no instructions";
            throw new InvalidOperationException(message);
        }

        return convertedInstructions;
    }
}