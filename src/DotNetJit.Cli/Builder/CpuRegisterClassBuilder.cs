using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class CpuRegisterClassBuilder
{
    private readonly TypeBuilder _typeBuilder;

    public TypeInfo Type => _typeBuilder;
    public FieldInfo Accumulator { get; }
    public FieldInfo XIndex { get; }
    public FieldInfo YIndex { get; }
    public FieldInfo StackPointer { get; }

    public CpuRegisterClassBuilder(string rootNamespace, ModuleBuilder module)
    {
        _typeBuilder = module.DefineType($"{rootNamespace}.CpuRegisters", TypeAttributes.Public);

        const FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.Static;
        Accumulator = _typeBuilder.DefineField("Accumulator", typeof(byte), attributes);
        XIndex = _typeBuilder.DefineField("XIndex", typeof(byte), attributes);
        YIndex = _typeBuilder.DefineField("YIndex", typeof(byte), attributes);
        StackPointer = _typeBuilder.DefineField("StackPointer", typeof(byte), attributes);

        _typeBuilder.CreateType();
    }
}