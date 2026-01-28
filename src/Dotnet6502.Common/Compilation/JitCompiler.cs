using System.Reflection.Emit;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler
{
    protected record ConvertedFunction(
        IReadOnlyList<ConvertedInstruction> Instructions,
        HashSet<ushort> AllowedSmcTargets,
        bool HandledAllKnownSmcTargets);

    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_0;

    private readonly Base6502Hal _hal;
    private readonly IReadOnlyList<IJitCustomizer> _jitCustomizers;
    private readonly MemoryBus _memoryBus;
    private readonly Queue<ushort> _ranMethods = new();
    private readonly Ir6502Interpreter _interpreter;
    private readonly SmcTracker _smcTracker = new();
    private readonly Dictionary<ushort, Patch> _patches = [];
    private readonly ExecutableMethodCache _executableMethodCache = new();
    private ushort _currentlyExecutingFunctionAddress;

    /// <summary>
    /// If true, the compiler will always use the interpreter instead of the JIT compiler
    /// </summary>
    public bool AlwaysUseInterpreter { get; set; }

    public JitCompiler(Base6502Hal hal, IJitCustomizer? jitCustomizer, MemoryBus memoryBus, Ir6502Interpreter interpreter)
    {
        _hal = hal;
        _hal.OnMemoryWritten = address =>
        {
            _executableMethodCache.MemoryChanged(address);
            _smcTracker.MemoryChanged(address);

            var isSelfModifying = _executableMethodCache.AddressPartOfFunctionInstructions(
                _currentlyExecutingFunctionAddress,
                address);

            if (isSelfModifying)
            {
                _smcTracker.MarkAsSelfModifying(hal.CurrentInstructionAddress, address);
            }

            return isSelfModifying;
        };

        _jitCustomizers = jitCustomizer != null
            ? [new StandardJitCustomizer(), jitCustomizer]
            : [new StandardJitCustomizer()];

        _memoryBus = memoryBus;
        _interpreter = interpreter;
    }

    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    public void RunMethod(ushort address)
    {
        int nextAddress = address;
        while (nextAddress >= 0)
        {
            var method = _executableMethodCache.GetMethodForAddress((ushort)nextAddress);
            if (method == null)
            {
                var function = DecompileFunction((ushort)nextAddress);
                var convertedFunction = GetIrInstructions(function);
                var customGenerators = _jitCustomizers.SelectMany(x => x.GetCustomIlGenerators())
                    .ToDictionary(x => x.Key, x => x.Value);

                if (AlwaysUseInterpreter || !convertedFunction.HandledAllKnownSmcTargets)
                {
                    _hal.DebugHook($"Using interpreter for 0x{nextAddress:X4}");
                    method = _interpreter.CreateExecutableMethod(convertedFunction.Instructions);
                }
                else
                {
                    _hal.DebugHook($"Using JIT for 0x{nextAddress:X4}");
                    method = ExecutableMethodGenerator.Generate(
                        $"func_{function.Address:X4}",
                        convertedFunction.Instructions,
                        customGenerators);
                }

                method = AddExecutableMethod(nextAddress, method, function, convertedFunction);
            }

            _ranMethods.Enqueue((ushort)nextAddress);
            while (_ranMethods.Count > 1000)
            {
                _ranMethods.Dequeue();
            }

            _hal.DebugHook($"Entering function 0x{nextAddress:X4}");
            _currentlyExecutingFunctionAddress = (ushort)nextAddress;
            nextAddress = method(_hal);
            _hal.DebugHook($"Exiting function 0x{_currentlyExecutingFunctionAddress:X4}");
        }

        if (_ranMethods.Count == 0)
        {
            _hal.DebugHook($"No functions executed");
        }
        else
        {
            var path = _ranMethods.Select(x => x.ToString("X4"))
                .Aggregate((x, y) => $"{x} -> {y}");

            _hal.DebugHook($"Function path: {path}");
        }
    }

    public void AddPatch(Patch patch)
    {
        _patches.Add(patch.FunctionEntryAddress, patch);
    }

    private DecompiledFunction DecompileFunction(ushort address)
    {
        var function = FunctionDecompiler.Decompile(address, _memoryBus.GetAllCodeRegions());
        if (function.OrderedInstructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} contained no instructions";
            throw new InvalidOperationException(message);
        }

        return function;
    }

    protected virtual ConvertedFunction GetIrInstructions(DecompiledFunction function)
    {
        var instructionConverterContext = new InstructionConverter.Context(
            function.JumpTargets,
            _smcTracker.GetTargets(function));

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
            var message = $"Function at address 0x{function.Address:X4} has no instructions";
            throw new InvalidOperationException(message);
        }

        var unhandledSmcTargetsExist = instructionConverterContext.SmcTargetAddresses
            .Where(x => !instructionConverterContext.HandledSmcTargets.Contains(x))
            .Any();

        return new ConvertedFunction(
            convertedInstructions,
            instructionConverterContext.HandledSmcTargets,
            !unhandledSmcTargetsExist);
    }

    protected ExecutableMethod AddExecutableMethod(
        int nextAddress,
        ExecutableMethod method,
        DecompiledFunction function,
        ConvertedFunction convertedFunction)
    {
        if (_patches.TryGetValue((ushort)nextAddress, out var patch))
        {
            method = patch.Apply(method);
        }

        _executableMethodCache.AddExecutableMethod(method, function, convertedFunction.AllowedSmcTargets);
        return method;
    }
}