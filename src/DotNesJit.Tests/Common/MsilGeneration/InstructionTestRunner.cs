using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using DotNesJit.Common.Compilation;
using DotNesJit.Common.Hal;
using Shouldly;

namespace DotNesJit.Tests.Common.MsilGeneration;

public class InstructionTestRunner
{
    private readonly string _testClassName;
    private readonly string _halFieldName;
    private readonly string _testMethodName;
    private readonly Assembly _assembly;

    public TestNesHal NesHal { get; } = new();

    public InstructionTestRunner(IReadOnlyList<NesIr.Instruction> instructions)
    {
         var (assemblyBuilder, mainClass, testMethod, halField) = SetupTestClass(instructions);
         _testClassName = mainClass.FullName!;
         _halFieldName = halField.Name;
         _testMethodName = testMethod.Name;

         // Load the assembly
         using var stream = new MemoryStream();
         assemblyBuilder.Save(stream);
         stream.Seek(0, SeekOrigin.Begin);
         _assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
    }

    public void RunTestMethod()
    {
        var mainClass = _assembly.GetType(_testClassName)!;
        var halField = mainClass.GetField(_halFieldName);
        var testMethod = mainClass.GetMethod(_testMethodName);

        halField.ShouldNotBeNull();
        testMethod.ShouldNotBeNull();

        halField.SetValue(null, NesHal);
        testMethod.Invoke(null, []);
    }

    private static (PersistedAssemblyBuilder, TypeInfo, MethodInfo, FieldInfo) SetupTestClass(
        IReadOnlyList<NesIr.Instruction> instructions)
    {
        var ns = $"nes_test_{Guid.NewGuid()}";
        var assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(ns),
            typeof(object).Assembly);

        var rootModule = assemblyBuilder.DefineDynamicModule("<Module>");
        var mainClass = rootModule.DefineType(ns, TypeAttributes.Public);
        var hardwareField = mainClass.DefineField(
            "Hardware",
            typeof(INesHal),
            FieldAttributes.Public | FieldAttributes.Static);

        var testMethod = mainClass.DefineMethod("RunTest", MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = testMethod.GetILGenerator();

        // We need to pull out all labels so they can be pre-defined, since they need to be
        // defined before they can be marked or referenced
        var ilLabels = instructions
            .OfType<NesIr.Label>()
            .ToDictionary(x => x.Name, x => ilGenerator.DefineLabel());

        var localCount = GetMaxLocalCount(instructions) + MsilGenerator.TemporaryLocalsRequired;
        for (var x = 0; x < localCount; x++)
        {
            ilGenerator.DeclareLocal(typeof(int));
        }

        var msilGenerator = new MsilGenerator(ilLabels);

        // TODO: Figure out how to support tests for the CallFunction IR instruction
        var context = new MsilGenerator.Context(ilGenerator, hardwareField, _ => null);

        foreach (var instruction in instructions)
        {
            ilGenerator.Emit(OpCodes.Ldstr, $"{instruction}");
            ilGenerator.Emit(OpCodes.Pop);

            msilGenerator.Generate(instruction, context);
        }

        ilGenerator.Emit(OpCodes.Ret);

        mainClass.CreateType();

        return (assemblyBuilder, mainClass, testMethod, hardwareField);
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
}