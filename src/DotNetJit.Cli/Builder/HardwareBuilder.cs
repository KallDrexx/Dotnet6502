using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class HardwareBuilder
{
    private readonly string _rootNamespace;

    public string TypeName => $"{_rootNamespace}.Hardware";
    public const string AccumulatorFieldName ="Accumulator";
    public const string XIndexFieldName = "XIndex";
    public const string YIndexFieldName = "YIndex";
    public const string StatusFieldName = "Status";
    public const string MemoryFieldName = "Memory";

    public HardwareBuilder(string rootNamespace)
    {
        _rootNamespace = rootNamespace;
    }

    public void AddCpuRegisterType(ModuleBuilder module)
    {
        var typeBuilder = module.DefineType(TypeName, TypeAttributes.Public);

        typeBuilder.DefineField(AccumulatorFieldName, typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        typeBuilder.DefineField(XIndexFieldName, typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        typeBuilder.DefineField(YIndexFieldName, typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        typeBuilder.DefineField(StatusFieldName, typeof(byte), FieldAttributes.Public | FieldAttributes.Static);

        var memoryField = typeBuilder.DefineField(
            MemoryFieldName,
            typeof(byte[]),
            FieldAttributes.Public | FieldAttributes.Static);

        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Any,
            []);

        var constructorGenerator = constructor.GetILGenerator();

        // Constructor needs to allocate the 64k memory block
        constructorGenerator.Emit(OpCodes.Ldc_I4, 0x10000);
        constructorGenerator.Emit(OpCodes.Newarr, typeof(byte));
        constructorGenerator.Emit(OpCodes.Stsfld, memoryField);
        constructorGenerator.Emit(OpCodes.Ret);

        typeBuilder.CreateType();
    }
}