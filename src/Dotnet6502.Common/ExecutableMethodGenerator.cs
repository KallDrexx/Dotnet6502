using System.Reflection;
using System.Reflection.Emit;

namespace Dotnet6502.Common;

/// <summary>
/// Generates an executable method from 6502 assembly instruction
/// </summary>
public static class ExecutableMethodGenerator
{
    public static ExecutableMethod Generate(
        string name,
        IReadOnlyList<ConvertedInstruction> instructions,
        IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator>? customIlGenerators = null)
    {
        return GenerateViaAssemblies(name, instructions, customIlGenerators);
        // return GenerateViaDynamicMethod(name, instructions, customIlGenerators);
    }

    private static ExecutableMethod GenerateViaDynamicMethod(
        string name,
        IReadOnlyList<ConvertedInstruction> instructions,
        IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator>? customIlGenerators)
    {
        var methodToCreate = new DynamicMethod(
            name,
            MethodAttributes.Static | MethodAttributes.Public,
            CallingConventions.Standard,
            typeof(void),
            [typeof(IJitCompiler), typeof(I6502Hal)],
            typeof(JitCompiler).Module,
            false);

        var ilGenerator = methodToCreate.GetILGenerator();
        GenerateMsil(ilGenerator, instructions, customIlGenerators);

        return methodToCreate.CreateDelegate<ExecutableMethod>();
    }

    private static ExecutableMethod GenerateViaAssemblies(
        string name,
        IReadOnlyList<ConvertedInstruction> instructions,
        IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator>? customIlGenerators)
    {
        var assemblyName = new AssemblyName($"assembly_for_{name}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule($"module_for_{name}");
        var typeBuilder = moduleBuilder.DefineType($"class_for_{name}", TypeAttributes.Public);
        var methodBuilder = typeBuilder.DefineMethod(
            name,
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(void),
            [typeof(IJitCompiler), typeof(I6502Hal)]);

        var ilGenerator = methodBuilder.GetILGenerator();
        GenerateMsil(ilGenerator, instructions, customIlGenerators);

        var constructedType = typeBuilder.CreateType();
        var method = constructedType.GetMethod(methodBuilder.Name);
        if (method == null)
        {
            throw new InvalidOperationException("Failed to get method from newly constructed type");
        }

        return (ExecutableMethod) Delegate.CreateDelegate(typeof(ExecutableMethod), method, true)!;
    }

    /// <summary>
    /// Generates complete MSIL for the specified set of instructions
    /// </summary>
    private static void GenerateMsil(ILGenerator ilGenerator,
        IReadOnlyList<ConvertedInstruction> instructions,
        IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator>? customIlGenerators = null)
    {
        // We need to pull out all labels so they can be pre-defined, since they need to be
        // defined before they can be marked or referenced
        var ilLabels = instructions
            .SelectMany(x => x.Ir6502Instructions)
            .OfType<Ir6502.Label>()
            .ToDictionary(x => x.Name, x => ilGenerator.DefineLabel());

        // Figure out how many locals this method will need and declare them.
        MsilGenerator.DeclareRequiredLocals(ilGenerator);

        // Declare locals required for instruction usage
        var localCount = GetMaxLocalCount(instructions.SelectMany(x => x.Ir6502Instructions).ToArray());
        for (var x = 0; x < localCount; x++)
        {
            ilGenerator.DeclareLocal(typeof(int));
        }

        var msilGenerator = new MsilGenerator(ilLabels, customIlGenerators);
        foreach (var instruction in instructions)
        {
            var debugInstruction = new Ir6502.StoreDebugString(instruction.OriginalInstruction.ToString());
            msilGenerator.Generate(debugInstruction, ilGenerator);
            foreach (var irInstruction in instruction.Ir6502Instructions)
            {
                msilGenerator.Generate(irInstruction, ilGenerator);
            }
        }

        ilGenerator.Emit(OpCodes.Ret);
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
}