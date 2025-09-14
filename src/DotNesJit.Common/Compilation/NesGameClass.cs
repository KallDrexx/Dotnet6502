using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.ComTypes;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation;

/// <summary>
/// Builds an NES game class (and outer assembly) to hold the decompiled 6502 instructions and the
/// NES hardware abstraction layer to be used.
/// </summary>
public class NesGameClass
{
    private record NesMethod(Function NesFunction, MethodBuilder Builder);

    private readonly PersistedAssemblyBuilder _assemblyBuilder;
    private readonly Dictionary<string, NesMethod> _methods = new();
    private readonly Decompiler _decompiler;

    public TypeBuilder Type { get; }
    public FieldInfo HardwareField { get; }

    public NesGameClass(string namespaceName, Decompiler decompiler, Disassembler disassembler)
    {
        _decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));

        _assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _assemblyBuilder.DefineDynamicModule("<Module>");

        Type = rootModule.DefineType($"{namespaceName}.Game", TypeAttributes.Public);
        HardwareField = Type.DefineField(
            "Hardware",
            typeof(INesHal),
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

    private static int GetMaxLocalCount(IReadOnlyList<NesIr.Instruction> instructions)
    {
        var largestLocalCount = 0;
        foreach (var instruction in instructions)
        {
            // Use reflection to find all value properties, so we don't have to manually switch through them
            // and maintain that.
            var valueProperties = instruction.GetType()
                .GetProperties()
                .Where(x => x.PropertyType == typeof(NesIr.Value))
                .ToArray();

            foreach (var property in valueProperties)
            {
                if (property.GetValue(instruction) is NesIr.Variable variable)
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

        var nesIrInstructions = new List<NesIr.Instruction>();
        foreach (var disassembledInstruction in disassembledInstructions)
        {
            nesIrInstructions.AddRange(
                InstructionConverter.Convert(
                    disassembledInstruction,
                    disassembler,
                    _decompiler));
        }

        // We need to pull out all labels so they can be pre-defined, since they need to be
        // defined before they can be marked or referenced
        var ilLabels = nesIrInstructions
            .OfType<NesIr.Label>()
            .ToDictionary(x => x.Name, x => ilGenerator.DefineLabel());

        var localCount = GetMaxLocalCount(nesIrInstructions) + MsilGenerator.TemporaryLocalsRequired;
        for (var x = 0; x < localCount; x++)
        {
            ilGenerator.DeclareLocal(typeof(int));
        }

        var msilGenerator = new MsilGenerator(ilLabels);
        foreach (var instruction in nesIrInstructions)
        {
            ilGenerator.Emit(OpCodes.Ldstr, $"{instruction}");
            ilGenerator.Emit(OpCodes.Pop);

            msilGenerator.Generate(instruction, ilGenerator, this);
        }
    }
}
