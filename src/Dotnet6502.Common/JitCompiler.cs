using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common;

/// <summary>
/// Compiles 6502 assembly functions based on a specified method entry point
/// on an as-needed basis.
/// </summary>
public class JitCompiler
{
    private delegate void CompiledMethod(JitCompiler jitCompiler, I6502Hal hal);
    private record ConvertedInstruction(
        DisassembledInstruction OriginalInstruction,
        IReadOnlyList<Ir6502.Instruction> Ir6502Instructions);

    private readonly Decompiler _decompiler;
    private readonly I6502Hal _hal;
    private readonly Dictionary<ushort, CompiledMethod> _compiledMethods = new();

    public JitCompiler(Decompiler decompiler, I6502Hal hal)
    {
        _decompiler = decompiler;
        _hal = hal;
    }

    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    public void RunMethod(ushort address)
    {
        if (!_compiledMethods.TryGetValue(address, out var method))
        {
            method = CompileMethod(address);
            _compiledMethods.Add(address, method);
        }

        method(this, _hal);
    }

    private static int GetMaxLocalCount(IReadOnlyList<Ir6502.Instruction> instructions)
    {
        var largestLocalCount = 0;
        foreach (var instruction in instructions)
        {
            // Use reflection to find all value properties, so we don't have to manually switch through them
            // and maintain that.
            var valueProperties = instruction.GetType()
                .GetProperties()
                .Where(x => x.PropertyType == typeof(Ir6502.Value))
                .ToArray();

            foreach (var property in valueProperties)
            {
                if (property.GetValue(instruction) is Ir6502.Variable variable)
                {
                    var variableCount = variable.Index + 1;
                    if (largestLocalCount < variableCount)
                    {
                        largestLocalCount = variableCount;
                    }
                }
            }
        }

        return largestLocalCount;
    }

    private CompiledMethod CompileMethod(ushort address)
    {
        var methodToCreate = new DynamicMethod(
            $"function_{address:X4}",
            MethodAttributes.Static | MethodAttributes.Public,
            CallingConventions.Any,
            typeof(void),
            [typeof(JitCompiler), typeof(I6502Hal)],
            typeof(JitCompiler).Module,
            false);

        var ilGenerator = methodToCreate.GetILGenerator();
        if (!_decompiler.Functions.TryGetValue(address, out var function))
        {
            var message = $"No known function exists at address {address:X4}";
            throw new InvalidOperationException(message);
        }

        var disassembledInstructions = _decompiler.Disassembler.Instructions
            .Where(x => function.Instructions.Contains(x.CPUAddress))
            .OrderBy(x => x.CPUAddress)
            .ToArray();

        // TODO: Functions collection is no longer needed, as each callFunction will convert to JIT calls
        var instructionConverterContext = new InstructionConverter.Context(
            _decompiler.Disassembler.Labels,
            _decompiler.Functions);

        var convertedInstructions = new List<ConvertedInstruction>();
        foreach (var disassembledInstruction in disassembledInstructions)
        {
            var irInstructions = InstructionConverter.Convert(disassembledInstruction, instructionConverterContext);

            // Prepend cycle count instruction. This has to be done first, otherwise it will get missed
            // by branch/jump calls.
            var instructionsWithCycleCount = irInstructions
                // TODO: Make this not specific to NES, probably by overriding instruction generation
                // .Prepend([new IncrementCycleCount(disassembledInstruction.Info.Cycles)])
                .ToArray();

            convertedInstructions.Add(new ConvertedInstruction(disassembledInstruction, instructionsWithCycleCount));
        }

        // We need to pull out all labels so they can be pre-defined, since they need to be
        // defined before they can be marked or referenced
        var ilLabels = convertedInstructions
            .SelectMany(x => x.Ir6502Instructions)
            .OfType<Ir6502.Label>()
            .ToDictionary(x => x.Name, x => ilGenerator.DefineLabel());

        var localCount = GetMaxLocalCount(convertedInstructions.SelectMany(x => x.Ir6502Instructions).ToArray());
        localCount += MsilGenerator.TemporaryLocalsRequired;

        for (var x = 0; x < localCount; x++)
        {
            ilGenerator.DeclareLocal(typeof(int));
        }

        // TODO: Add support
        // var customIlGenerators = new Dictionary<Type, MsilGenerator.CustomIlGenerator>
        // {
        //     { typeof(IncrementCycleCount), CreateCycleCountIlGenerator() }
        // };
        var msilGenerator = new MsilGenerator(ilLabels, []);
        var context = new MsilGenerator.Context(ilGenerator, HardwareField, GetMethodInfo);
        foreach (var convertedInstruction in convertedInstructions)
        {
            ilGenerator.Emit(OpCodes.Ldstr, $"{convertedInstruction.OriginalInstruction}");
            ilGenerator.Emit(OpCodes.Pop);
            foreach (var irInstruction in convertedInstruction.Ir6502Instructions)
            {
                msilGenerator.Generate(irInstruction, context);
            }
        }

        ilGenerator.Emit(OpCodes.Ret);



    }
}