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
    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_1;

    private readonly Decompiler _decompiler;
    private readonly Base6502Hal _hal;
    private readonly IReadOnlyList<IJitCustomizer> _jitCustomizers;
    private readonly IMemoryMap _memoryMap;
    protected Dictionary<ushort, ExecutableMethod> CompiledMethods { get; } = new();

    public JitCompiler(Decompiler decompiler, Base6502Hal hal, IJitCustomizer? jitCustomizer, IMemoryMap memoryMap)
    {
        _hal = hal;
        _decompiler = decompiler;
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

            _hal.DebugHook($"Entering function 0x{address:X4}");
            nextAddress = method(this, _hal);
            _hal.DebugHook($"Exiting function 0x{address:X4}");
        }
    }

    protected virtual IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        var region = _memoryMap.GetCodeRegions()
            .Where(x => x.BaseAddress <= address)
            .Where(x => x.BaseAddress + x.Bytes.Length > address)
            .FirstOrDefault();

        if (region == null)
        {
            var message = $"No known code region contains the address 0x{address:X4}";
            throw new InvalidOperationException(message);
        }

        var disassembler = new Disassembler(region.BaseAddress, region.Bytes);
        disassembler.AddEntyPoint(address);
        disassembler.Disassemble();

        // Rom info only needed for string functions, which we don't use
        var decompiler = new Decompiler(_decompiler.ROMInfo, disassembler);
        decompiler.Decompile();

        if (!decompiler.Functions.TryGetValue(address, out var function))
        {
            var message = $"Address 0x{address:X4} did not contain a decompilable function";
            throw new InvalidOperationException(message);
        }

        if (function.Instructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} contained no instructions";
            throw new InvalidOperationException(message);
        }

        var disassembledInstructions = disassembler.Instructions
            .Where(x => function.Instructions.Contains(x.CPUAddress))
            .OrderBy(x => x.CPUAddress)
            .ToArray();

        var preInstructions = disassembledInstructions.Where(x => x.CPUAddress < address);
        var postInstructions = disassembledInstructions.Where(x => x.CPUAddress >= address);
        var orderedInstructions = postInstructions.Concat(preInstructions).ToArray();

        var instructionConverterContext = new InstructionConverter.Context(disassembler.Labels);

        // Convert each 6502 instruction into one or more IR instructions
        IReadOnlyList<ConvertedInstruction> convertedInstructions = orderedInstructions
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