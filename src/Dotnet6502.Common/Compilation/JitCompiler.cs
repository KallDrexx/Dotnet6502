using System.Reflection.Emit;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler<THal> where THal : I6502Hal
{
    protected record ConvertedFunction(
        IReadOnlyList<ConvertedInstruction> Instructions,
        HashSet<ushort> AllowedSmcTargets,
        bool HandledAllKnownSmcTargets,
        IReadOnlyList<Ir6502.Label> JumpTableLabels);

    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_0;
    public static readonly OpCode LoadJumpIndexArg = OpCodes.Ldarg_1;

    private readonly THal _hal;
    private readonly IReadOnlyList<IJitCustomizer<THal>> _jitCustomizers;
    private readonly IMemoryBus _memoryBus;
    private readonly Queue<ushort> _ranMethods = new();
    private readonly Ir6502Interpreter _interpreter;
    private readonly SmcTracker _smcTracker = new();
    private readonly Dictionary<ushort, Patch> _patches = [];
    private readonly ExecutableMethodCache<THal> _executableMethodCache = new();
    private ushort _currentlyExecutingFunctionAddress;

    /// <summary>
    /// If true, the compiler will always use the interpreter instead of the JIT compiler
    /// </summary>
    public bool AlwaysUseInterpreter { get; set; }

    /// <summary>
    /// The number of times a method was executed but was not yet compiled
    /// </summary>
    public int MethodNotCompiledCount { get; private set; }

    /// <summary>
    /// The number of times a method was called
    /// </summary>
    public int MethodCallCount { get; private set; }

    /// <summary>
    /// If true, then the HAL debug hook will be invoked by compiled methods.
    /// </summary>
    public bool AddDebugHooks { get; init; }

    public JitCompiler(THal hal, IJitCustomizer<THal>? jitCustomizer, IMemoryBus memoryBus, Ir6502Interpreter interpreter)
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
            ? [new StandardJitCustomizer<THal>(), jitCustomizer]
            : [new StandardJitCustomizer<THal>()];

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
            MethodCallCount++;
            var firstInstructionIndex = 0;
            var method = _executableMethodCache.GetMethodForAddress((ushort)nextAddress);
            if (method == null)
            {
                (method, firstInstructionIndex) = CreateExecutableMethod(nextAddress);
            }

            _ranMethods.Enqueue((ushort)nextAddress);
            while (_ranMethods.Count > 1000)
            {
                _ranMethods.Dequeue();
            }

            if (AddDebugHooks)
            {
                _hal.DebugHook($"Entering function 0x{nextAddress:X4}");
            }

            _currentlyExecutingFunctionAddress = (ushort)nextAddress;
            nextAddress = method(_hal, firstInstructionIndex);

            if (AddDebugHooks)
            {
                _hal.DebugHook($"Exiting function 0x{_currentlyExecutingFunctionAddress:X4}");
            }
        }

        if (_ranMethods.Count == 0)
        {
            if (AddDebugHooks)
            {
                _hal.DebugHook($"No functions executed");
            }
        }
        else
        {
            var path = _ranMethods.Select(x => x.ToString("X4"))
                .Aggregate((x, y) => $"{x} -> {y}");

            if (AddDebugHooks)
            {
                _hal.DebugHook($"Function path: {path}");
            }
        }
    }

    public void AddPatch(Patch patch)
    {
        _patches.Add(patch.FunctionEntryAddress, patch);
    }

    private (ExecutableMethod<THal> Method, int FirstIndex) CreateExecutableMethod(int nextAddress)
    {
        ExecutableMethod<THal> method;

        // Does the address we are trying to jump through exist within an existing cached function?
        var existingFunction = _executableMethodCache.GetFunctionAddressForInstruction((ushort)nextAddress);
        if (existingFunction != null)
        {
            var cachedMethod = _executableMethodCache.GetMethodForAddress(existingFunction.Value.FunctionAddress);
            if (cachedMethod == null)
            {
                var message = $"Instruction 0x{nextAddress:X4} is listed as part of function " +
                              $"0x{existingFunction.Value.FunctionAddress:X4}, but that function is not cached";

                throw new InvalidOperationException(message);
            }

            return (cachedMethod, existingFunction.Value.InstructionIndex);
        }

        MethodNotCompiledCount++;

        var function = DecompileFunction((ushort)nextAddress);
        var convertedFunction = GetIrInstructions(function);
        var customGenerators = _jitCustomizers.SelectMany(x => x.GetCustomIlGenerators())
            .ToDictionary(x => x.Key, x => x.Value);

        if (AlwaysUseInterpreter || !convertedFunction.HandledAllKnownSmcTargets)
        {
            if (AddDebugHooks)
            {
                _hal.DebugHook($"Using interpreter for 0x{nextAddress:X4}");
            }

            method = _interpreter.CreateExecutableMethod<THal>(convertedFunction.Instructions);
        }
        else
        {
            if (AddDebugHooks)
            {
                _hal.DebugHook($"Using JIT for 0x{nextAddress:X4}");
            }

            method = ExecutableMethodGenerator<THal>.Generate(
                $"func_{function.Address:X4}",
                convertedFunction.Instructions,
                convertedFunction.JumpTableLabels,
                customGenerators);
        }

        method = AddExecutableMethod(nextAddress, method, function, convertedFunction);
        return (method, 0);
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

        // Add an index based label for the jump table
        var jumpTableLabels = new List<Ir6502.Label>();
        for (var x = 0; x < convertedInstructions.Count; x++)
        {
            var identifier = new Ir6502.Identifier($"{function.Address:X4}_index_{x:000000}");
            var label = new Ir6502.Label(identifier);
            jumpTableLabels.Add(label);

            convertedInstructions[x].Ir6502Instructions.Insert(0, label);
        }

        var unhandledSmcTargetsExist = instructionConverterContext.SmcTargetAddresses
            .Where(x => !instructionConverterContext.HandledSmcTargets.Contains(x))
            .Any();

        return new ConvertedFunction(
            convertedInstructions,
            instructionConverterContext.HandledSmcTargets,
            !unhandledSmcTargetsExist,
            jumpTableLabels);
    }

    protected ExecutableMethod<THal> AddExecutableMethod(
        int nextAddress,
        ExecutableMethod<THal> method,
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
