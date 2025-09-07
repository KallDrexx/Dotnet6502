using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class HardwareBuilder
{
    private readonly TypeBuilder _typeBuilder;

    public TypeInfo Type => _typeBuilder;
    public FieldInfo Accumulator { get; }
    public FieldInfo XIndex { get; }
    public FieldInfo YIndex { get; }
    public FieldInfo Memory { get; }

    // Status fields
    public FieldInfo CarryFlag { get; }
    public FieldInfo ZeroFlag { get; }
    public FieldInfo InterruptDisableFlag { get; }
    public FieldInfo DecimalFlag { get; }
    public FieldInfo OverflowFlag { get; }
    public FieldInfo NegativeFlag { get; }
    public FieldInfo BreakFlag { get; }

    public HardwareBuilder(string rootNamespace, ModuleBuilder module)
    {
        _typeBuilder = module.DefineType($"{rootNamespace}.NesHardware", TypeAttributes.Public);

        Accumulator = _typeBuilder.DefineField("Accumulator", typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        XIndex = _typeBuilder.DefineField("XIndex", typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        YIndex = _typeBuilder.DefineField("YIndex", typeof(byte), FieldAttributes.Public | FieldAttributes.Static);
        CarryFlag = _typeBuilder.DefineField("Carry", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        ZeroFlag = _typeBuilder.DefineField("Zero", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        DecimalFlag = _typeBuilder.DefineField("Decimal", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        OverflowFlag = _typeBuilder.DefineField("Overflow", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        NegativeFlag = _typeBuilder.DefineField("Negative", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        BreakFlag = _typeBuilder.DefineField("Break", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        InterruptDisableFlag = _typeBuilder.DefineField(
            "InterruptDisable",
            typeof(bool),
            FieldAttributes.Public | FieldAttributes.Static);

        Memory = _typeBuilder.DefineField(
            "Memory",
            typeof(byte[]),
            FieldAttributes.Public | FieldAttributes.Static);

        var constructor = _typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Any,
            []);

        var constructorGenerator = constructor.GetILGenerator();

        // Constructor needs to allocate the 64k memory block
        constructorGenerator.Emit(OpCodes.Ldc_I4, 0x10000);
        constructorGenerator.Emit(OpCodes.Newarr, typeof(byte));
        constructorGenerator.Emit(OpCodes.Stsfld, Memory);
        constructorGenerator.Emit(OpCodes.Ret);

        _typeBuilder.CreateType();
    }
}