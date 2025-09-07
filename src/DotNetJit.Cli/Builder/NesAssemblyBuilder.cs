using System.Reflection;
using System.Reflection.Emit;
using DotNetJit.Cli.Builder.InstructionHandlers;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder;

public class NesAssemblyBuilder
{
    private readonly PersistedAssemblyBuilder _builder;
    private readonly Dictionary<ushort, FieldInfo> _variableFields = new();
    private readonly Dictionary<ushort, MethodInfo> _methods = new();
    private readonly TypeBuilder _gameClassBuilder;
    private readonly Dictionary<string, InstructionHandler> _instructionHandlers;

    public HardwareBuilder Hardware { get; }

    public Dictionary<ushort, FieldInfo> VariableFields => _variableFields;

    public NesAssemblyBuilder(string namespaceName, Decompiler decompiler)
    {
        _instructionHandlers = GetType().Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract)
            .Where(x => !x.IsInterface)
            .Where(x => typeof(InstructionHandler).IsAssignableFrom(x))
            .Select(x => (InstructionHandler)Activator.CreateInstance(x)!)
            .SelectMany(x => x.Mnemonics.Select(y => new { Mnemonic = y, Instance = x}))
            .ToDictionary(x => x.Mnemonic, x => x.Instance);

        _builder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _builder.DefineDynamicModule("<Module>");
        Hardware = new HardwareBuilder(namespaceName, rootModule);

        _gameClassBuilder = rootModule.DefineType($"{namespaceName}.Game", TypeAttributes.Public);
        AddGameVariables(decompiler);
        AddFunctions(decompiler);

        _gameClassBuilder.CreateType();
    }

    public void Save(Stream outputStream)
    {
        _builder.Save(outputStream);
    }

    private void AddGameVariables(Decompiler decompiler)
    {
        foreach (var variable in decompiler.Variables.Values)
        {
            var fieldType = variable.Type switch
            {
                // I think pointers are just 8 bit values? Not sure what arrays in nes would mean.
                VariableType.Word => typeof(ushort),
                _ => typeof(byte),
            };

            var field = _gameClassBuilder.DefineField(
                variable.Name,
                fieldType,
                FieldAttributes.Public | FieldAttributes.Static);

            _variableFields.Add(variable.Address, field);
        }
    }

    private void AddFunctions(Decompiler decompiler)
    {
        foreach (var function in decompiler.Functions.Values)
        {
            _methods.Add(function.Address, GenerateMethod(function, decompiler));
        }
    }

    private MethodBuilder GenerateMethod(Function function, Decompiler decompiler)
    {
        var method = _gameClassBuilder.DefineMethod(function.Name, MethodAttributes.Public | MethodAttributes.Static);
        var ilGenerator = method.GetILGenerator();

        var sortedBlocks = decompiler.CodeBlocks.Values.OrderBy(x => x.StartAddress);
        foreach (var codeBlock in sortedBlocks)
        {
            if (!function.Instructions.Contains(codeBlock.StartAddress))
            {
                continue;
            }

            foreach (var instruction in codeBlock.Instructions)
            {
                GenerateIl(ilGenerator, instruction);
            }
        }

        ilGenerator.Emit(OpCodes.Ret);

        return method;
    }

    private void GenerateIl(ILGenerator ilGenerator, DisassembledInstruction instruction)
    {
        if (!_instructionHandlers.TryGetValue(instruction.Info.Mnemonic, out var handler))
        {
            // throw new NotSupportedException(instruction.ToString());
            ilGenerator.EmitWriteLine($"Skipping {instruction}");
            return;
        }

        ilGenerator.EmitWriteLine(instruction.ToString());

        handler.Handle(ilGenerator, instruction, this);
    }
}