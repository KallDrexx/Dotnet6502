using System.Reflection.Emit;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler : IJitCompiler
{
    public static readonly OpCode LoadJitCompilerArg = OpCodes.Ldarg_0;
    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_1;

    private readonly Decompiler _decompiler;
    private readonly Base6502Hal _hal;
    private readonly IJitCustomizer? _jitCustomizer;
    private readonly Dictionary<ushort, ExecutableMethod> _compiledMethods = new();

    public JitCompiler(Decompiler decompiler, Base6502Hal hal, IJitCustomizer? jitCustomizer)
    {
        _hal = hal;
        _decompiler = decompiler;
        _jitCustomizer = jitCustomizer;
    }

    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    public void RunMethod(ushort address)
    {
        if (!_compiledMethods.TryGetValue(address, out var method))
        {
            var instructions = GetIrInstructions(address);
            if (instructions.Count == 0)
            {
                var message = $"Function at address 0x{address:X4} has no instructions";
                throw new InvalidOperationException(message);
            }

            var customGenerators = _jitCustomizer?.GetCustomIlGenerators();
            method = ExecutableMethodGenerator.Generate($"func_{address:X4}", instructions, customGenerators);
            _compiledMethods.Add(address, method);
        }

        _hal.DebugHook($"Entering function 0x{address:X4}");
        method(this, _hal);
        _hal.DebugHook($"Exiting function 0x{address:X4}");
    }

    private IReadOnlyList<ConvertedInstruction> GetIrInstructions(ushort address)
    {
        if (!_decompiler.Functions.TryGetValue(address, out var function))
        {
            Console.WriteLine($"Calling unknown function at {address:X4}. Attempting to compile it");
            _decompiler.Disassembler.AddEntyPoint(address);
            _decompiler.Disassembler.Disassemble();
            _decompiler.Decompile();

            if (!_decompiler.Functions.TryGetValue(address, out function))
            {
                var message = $"No known function exists at address {address:X4}";
                throw new InvalidOperationException(message);
            }
        }

        var disassembledInstructions = _decompiler.Disassembler.Instructions
            .Where(x => function.Instructions.Contains(x.CPUAddress))
            .OrderBy(x => x.CPUAddress)
            .ToArray();

        var instructionConverterContext = new InstructionConverter.Context(_decompiler.Disassembler.Labels);

        // Convert each 6502 instruction into one or more IR instructions
        IReadOnlyList<ConvertedInstruction> convertedInstructions = disassembledInstructions
            .Select(x => new ConvertedInstruction(x, InstructionConverter.Convert(x, instructionConverterContext)))
            .ToArray();

        // Mutate the instructions based on the JIT customizations being requested
        if (_jitCustomizer != null)
        {
            convertedInstructions = _jitCustomizer.MutateInstructions(convertedInstructions);
        }

        return convertedInstructions;
    }
}