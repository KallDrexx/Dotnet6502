using System.Diagnostics;
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
    public static readonly OpCode LoadHalArg = OpCodes.Ldarg_0;

    private readonly Base6502Hal _hal;
    private readonly IReadOnlyList<IJitCustomizer> _jitCustomizers;
    private readonly MemoryBus _memoryBus;
    private readonly Queue<ushort> _ranMethods = new();
    private readonly Ir6502Interpreter _interpreter;
    protected readonly ExecutableMethodCache ExecutableMethodCache = new();
    private ushort _currentlyExecutingAddress;

    public JitCompiler(Base6502Hal hal, IJitCustomizer? jitCustomizer, MemoryBus memoryBus, Ir6502Interpreter interpreter)
    {
        _hal = hal;
        _hal.OnMemoryWritten = address =>
        {
            ExecutableMethodCache.MemoryChanged(address);

            return AddressPartOfCurrentlyRunningFunction(address);
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
            var method = ExecutableMethodCache.GetMethodForAddress((ushort)nextAddress);
            if (method == null)
            {
                var function = DecompileFunction((ushort)nextAddress);
                var instructions = GetIrInstructions(function);
                var customGenerators = _jitCustomizers.SelectMany(x => x.GetCustomIlGenerators())
                    .ToDictionary(x => x.Key, x => x.Value);

                if (function.IsSelfModifying)
                {
                    // Self-modifying routines (e.g. GETCHR-style operand patching) can repeatedly
                    // invalidate JITed code, so we route them through the interpreter instead.
                    _hal.DebugHook(
                        $"Detected self-modifying code at 0x{function.Address:X4}; routing through interpreter.");
                    method = _interpreter.CreateExecutableMethod(instructions);
                }
                else
                {
                    method = ExecutableMethodGenerator.Generate(
                        $"func_{function.Address:X4}",
                        instructions,
                        customGenerators);
                }
                ExecutableMethodCache.AddExecutableMethod(method, function);
            }

            _ranMethods.Enqueue((ushort)nextAddress);
            while (_ranMethods.Count > 1000)
            {
                _ranMethods.Dequeue();
            }

            _hal.DebugHook($"Entering function 0x{nextAddress:X4}");
            _currentlyExecutingAddress = (ushort)nextAddress;
            nextAddress = method(_hal);
            _hal.DebugHook($"Exiting function 0x{_currentlyExecutingAddress:X4}");
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

    /// <summary>
    /// Checks if the specified address is part of the instruction set for the currently executing function
    /// </summary>
    public bool AddressPartOfCurrentlyRunningFunction(ushort address)
    {
        return ExecutableMethodCache.AddressPartOfFunctionInstructions(_currentlyExecutingAddress, address);
    }

    protected virtual DecompiledFunction DecompileFunction(ushort address)
    {
        var function = FunctionDecompiler.Decompile(address, _memoryBus.GetAllCodeRegions());
        if (function.OrderedInstructions.Count == 0)
        {
            var message = $"Function at address 0x{address:X4} contained no instructions";
            throw new InvalidOperationException(message);
        }

        if (SelfModifyingCodeDetector.TryDetect(function, out var affectedAddresses))
        {
            Console.WriteLine($"Function 0x{function.Address:X4} has self-modifying code");
            function.IsSelfModifying = true;
            _hal.DebugHook(
                $"Self-modifying pattern detected in function 0x{function.Address:X4} at " +
                $"{string.Join(", ", affectedAddresses.Select(addr => $"0x{addr:X4}"))}");
        }

        return function;
    }

    protected virtual IReadOnlyList<ConvertedInstruction> GetIrInstructions(DecompiledFunction function)
    {
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
            var message = $"Function at address 0x{function.Address:X4} has no instructions";
            throw new InvalidOperationException(message);
        }

        return convertedInstructions;
    }
}