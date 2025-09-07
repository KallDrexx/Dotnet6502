using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.Decompilation;

namespace DotNetJit.Cli.Builder;

public class VariableTrackerBuilder
{
    private readonly TypeBuilder _typeBuilder;
    private readonly Dictionary<ushort, FieldInfo> _variableFields = new();

    public TypeInfo Type => _typeBuilder;

    public VariableTrackerBuilder(
        string rootNamespace,
        ModuleBuilder module,
        IReadOnlyDictionary<ushort, Variable> variables)
    {
        _typeBuilder = module.DefineType($"{rootNamespace}.VariableTracker", TypeAttributes.Public);

        foreach (var variable in variables.Values)
        {
            var fieldType = variable.Type switch
            {
                // I think pointers are just 8 bit values? Not sure what arrays in nes would mean.
                VariableType.Word => typeof(ushort),
                _ => typeof(byte),
            };

            var field = _typeBuilder.DefineField(
                variable.Name,
                fieldType,
                FieldAttributes.Public | FieldAttributes.Static);

            _variableFields.Add(variable.Address, field);
        }

        _typeBuilder.CreateType();
    }
}