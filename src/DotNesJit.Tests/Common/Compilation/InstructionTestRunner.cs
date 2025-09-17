using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using DotNesJit.Common.Compilation;
using DotNesJit.Common.Hal;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation;

public class InstructionTestRunner
{
    private readonly string _testClassName;
    private readonly string _halFieldName;
    private readonly string _testMethodName;
    private readonly Assembly _assembly;
    private readonly Dictionary<string, MethodInfo> _callableMethods;

    public TestNesHal NesHal { get; } = new();

    public InstructionTestRunner(IReadOnlyList<NesIr.Instruction> instructions)
        : this(instructions, [])
    {
    }

    public InstructionTestRunner(IReadOnlyList<NesIr.Instruction> instructions, IReadOnlyList<string> callableFunctionNames)
    {
         var (assemblyBuilder, mainClass, testMethod, halField, _) = SetupTestClass(instructions, callableFunctionNames);
         _testClassName = mainClass.FullName!;
         _halFieldName = halField.Name;
         _testMethodName = testMethod.Name;

         // Load the assembly
         using var stream = new MemoryStream();
         assemblyBuilder.Save(stream);
         stream.Seek(0, SeekOrigin.Begin);
         _assembly = AssemblyLoadContext.Default.LoadFromStream(stream);

         // Get the actual MethodInfo instances from the loaded assembly
         var loadedClass = _assembly.GetType(_testClassName)!;
         _callableMethods = callableFunctionNames.ToDictionary(
             name => name,
             name => loadedClass.GetMethod(name)!);
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

    public (ushort Address, byte ExpectedValue) GetCallableMethodSignature(string functionName, IReadOnlyList<string> allFunctionNames)
    {
        var uniqueValue = (byte)(Math.Abs(functionName.GetHashCode()) % 256);
        var memoryAddress = (ushort)(0x8000 + (allFunctionNames.ToList().IndexOf(functionName) * 2));
        return (memoryAddress, uniqueValue);
    }

    private static (PersistedAssemblyBuilder, TypeInfo, MethodInfo, FieldInfo, Dictionary<string, MethodInfo>) SetupTestClass(
        IReadOnlyList<NesIr.Instruction> instructions, IReadOnlyList<string> callableFunctionNames)
    {
        var ns = $"nes_test_{Guid.NewGuid()}";
        var assemblyBuilder = new PersistedAssemblyBuilder(
            new AssemblyName(ns),
            typeof(object).Assembly);

        var rootModule = assemblyBuilder.DefineDynamicModule("<Module>");
        var mainClass = rootModule.DefineType($"{ns}.TestClass", TypeAttributes.Public);
        var hardwareField = mainClass.DefineField(
            "Hardware",
            typeof(INesHal),
            FieldAttributes.Public | FieldAttributes.Static);

        var testMethod = mainClass.DefineMethod("RunTest", MethodAttributes.Public | MethodAttributes.Static);

        // Create callable methods that can be invoked by CallFunction instructions
        var callableMethods = new Dictionary<string, MethodInfo>();
        foreach (var functionName in callableFunctionNames)
        {
            var callableMethod = mainClass.DefineMethod(functionName, MethodAttributes.Public | MethodAttributes.Static);
            var callableIlGenerator = callableMethod.GetILGenerator();

            // Each callable method writes a unique value to memory to indicate it was called
            // Use the function name's hash code as a unique identifier
            var uniqueValue = Math.Abs(functionName.GetHashCode()) % 256;
            var memoryAddress = 0x8000 + (callableFunctionNames.ToList().IndexOf(functionName) * 2);

            // Call NesHal.WriteMemory(address, value) to create observable side effect
            var writeMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.WriteMemory))!;
            callableIlGenerator.Emit(OpCodes.Ldsfld, hardwareField);
            callableIlGenerator.Emit(OpCodes.Ldc_I4, memoryAddress);
            callableIlGenerator.Emit(OpCodes.Ldc_I4, uniqueValue);
            callableIlGenerator.Emit(OpCodes.Conv_U1); // Convert to byte
            callableIlGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
            callableIlGenerator.Emit(OpCodes.Ret);

            callableMethods[functionName] = callableMethod;
        }

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

        // Support CallFunction IR instruction tests by providing callable methods
        var context = new MsilGenerator.Context(ilGenerator, hardwareField, name => callableMethods.GetValueOrDefault(name));

        foreach (var instruction in instructions)
        {
            msilGenerator.Generate(instruction, context);
        }

        ilGenerator.Emit(OpCodes.Ret);

        mainClass.CreateType();

        return (assemblyBuilder, mainClass, testMethod, hardwareField, callableMethods);
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