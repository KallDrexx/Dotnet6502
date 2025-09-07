using System.Reflection;
using System.Reflection.Emit;
using DotNetJit.Cli.Builder.InstructionHandlers;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder;

public class NesAssemblyBuilder
{
    private readonly PersistedAssemblyBuilder _builder;
    private readonly Dictionary<ushort, MethodInfo> _methods = new();
    private readonly GameClass _gameClass;
    private readonly Dictionary<string, InstructionHandler> _instructionHandlers;

    public HardwareBuilder Hardware { get; }

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

        _gameClass = SetupGameClass(rootModule, namespaceName, decompiler);
        AddFunctions(decompiler);

        _gameClass.Type.CreateType();
    }

    public void Save(Stream outputStream)
    {
        _builder.Save(outputStream);
    }

    private GameClass SetupGameClass(ModuleBuilder module, string namespaceName, Decompiler decompiler)
    {
        var builder = module.DefineType($"{namespaceName}.Game", TypeAttributes.Public);
        var hardwareField = builder.DefineField(
            "Hardware",
            typeof(NesHardware),
            FieldAttributes.Public | FieldAttributes.Static);

        return new GameClass
        {
            Type = builder,
            HardwareField = hardwareField,
            CpuRegisters = Hardware,
        };
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
        var method = _gameClass.Type.DefineMethod(function.Name, MethodAttributes.Public | MethodAttributes.Static);
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

        handler.Handle(ilGenerator, instruction, _gameClass);
    }
}