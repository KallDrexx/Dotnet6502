using System.Reflection;
using System.Reflection.Emit;

namespace DotNesJit.Common.Compilation;

public class CpuRegisterClassBuilder
{
    public FieldInfo Accumulator { get; }
    public FieldInfo XIndex { get; }
    public FieldInfo YIndex { get; }
    public FieldInfo StackPointer { get; }

    public CpuRegisterClassBuilder(string rootNamespace, ModuleBuilder module)
    {
        var typeBuilder = module.DefineType($"{rootNamespace}.CpuRegisters", TypeAttributes.Public);

        const FieldAttributes attributes = FieldAttributes.Public | FieldAttributes.Static;
        Accumulator = typeBuilder.DefineField("Accumulator", typeof(byte), attributes);
        XIndex = typeBuilder.DefineField("XIndex", typeof(byte), attributes);
        YIndex = typeBuilder.DefineField("YIndex", typeof(byte), attributes);
        StackPointer = typeBuilder.DefineField("StackPointer", typeof(byte), attributes);

        typeBuilder.CreateType();
    }
}