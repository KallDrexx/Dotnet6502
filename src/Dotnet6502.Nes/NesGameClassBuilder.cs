using System.Reflection;
using System.Reflection.Emit;
using Dotnet6502.Common;
using Dotnet6502.Common.Compilation;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Nes;

/// <summary>
/// Builds an NES game class (and outer assembly) to hold the decompiled 6502 instructions and the
/// NES hardware abstraction layer to be used.
/// </summary>
public class NesGameClassBuilder
{
    private record NesMethod(Function NesFunction, MethodBuilder Builder);

    private record ConvertedInstruction(
        DisassembledInstruction OriginalInstruction,
        IReadOnlyList<Ir6502.Instruction> Ir6502Instructions);

    private record IncrementCycleCount(int Cycles) : Ir6502.Instruction;

    private readonly PersistedAssemblyBuilder _assemblyBuilder;
    private readonly Dictionary<string, NesMethod> _methods = new();
    private readonly Decompiler _decompiler;

    public TypeBuilder Type { get; }
    public FieldInfo HardwareField { get; }

    public NesGameClassBuilder(string namespaceName, Decompiler decompiler, Disassembler disassembler)
    {
        _decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));

        _assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _assemblyBuilder.DefineDynamicModule("<Module>");

        Type = rootModule.DefineType($"{namespaceName}.Game", TypeAttributes.Public);
        HardwareField = Type.DefineField(
            "Hardware",
            typeof(I6502Hal),
            FieldAttributes.Public | FieldAttributes.Static);

        // We need to create all the method builders before we generate the method contents
        // so that the `MethodInfo` is available to pass into one method that calls another,
        // without having to do complex dependency management.
        CreateMethodBuilders(decompiler);

        foreach (var method in _methods.Values)
        {
            GenerateMethodIl(method, disassembler);
        }

        Type.CreateType();
    }

    public void WriteAssemblyTo(Stream stream)
    {
        _assemblyBuilder.Save(stream);
    }

    public MethodInfo? GetMethodInfo(string name)
    {
        return _methods.GetValueOrDefault(name)?.Builder;
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

    private void CreateMethodBuilders(Decompiler decompiler)
    {
        foreach (var function in decompiler.Functions.Values)
        {
            var method = Type.DefineMethod(
                function.Name,
                MethodAttributes.Public | MethodAttributes.Static);

            _methods.Add(function.Name, new NesMethod(function, method));
        }
    }

    private void GenerateMethodIl(NesMethod nesMethod, Disassembler disassembler)
    {
        var ilGenerator = nesMethod.Builder.GetILGenerator();
        var disassembledInstructions = _decompiler.Disassembler.Instructions
            .Where(inst => nesMethod.NesFunction.Instructions.Contains(inst.CPUAddress))
            .OrderBy(inst => inst.CPUAddress)
            .ToArray();

        var instructionConverterContext = new InstructionConverter.Context(disassembler.Labels, _decompiler.Functions);
        var convertedInstructions = new List<ConvertedInstruction>();
        foreach (var disassembledInstruction in disassembledInstructions)
        {
            var nesIrInstructions = InstructionConverter.Convert(disassembledInstruction, instructionConverterContext);

            // Prepend cycle count instruction. This has to be done first, otherwise it will get missed
            // by branch/jump calls.
            var instructionsWithCycleCount = new List<Ir6502.Instruction>(
                [new IncrementCycleCount(disassembledInstruction.Info.Cycles)]
            ).Concat(nesIrInstructions).ToArray();

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

        var customIlGenerators = new Dictionary<Type, MsilGenerator.CustomIlGenerator>
        {
            { typeof(IncrementCycleCount), CreateCycleCountIlGenerator() }
        };
        var msilGenerator = new MsilGenerator(ilLabels, customIlGenerators);
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

    private static MsilGenerator.CustomIlGenerator CreateCycleCountIlGenerator()
    {
        return (instruction, context) =>
        {
            if (instruction is IncrementCycleCount incrementCycleCount)
            {
                // Load the hardware field
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);

                // Cast from I6502Hal interface to NesHal concrete type
                context.IlGenerator.Emit(OpCodes.Castclass, typeof(NesHal));

                // Load the cycle count as a constant
                context.IlGenerator.Emit(OpCodes.Ldc_I4, incrementCycleCount.Cycles);

                // Call NesHal.IncrementCpuCycleCount(int count)
                var incrementMethod = typeof(NesHal).GetMethod(nameof(NesHal.IncrementCpuCycleCount))!;
                context.IlGenerator.Emit(OpCodes.Callvirt, incrementMethod);
            }
            else
            {
                throw new NotSupportedException(instruction.GetType().FullName);
            }
        };
    }
}
