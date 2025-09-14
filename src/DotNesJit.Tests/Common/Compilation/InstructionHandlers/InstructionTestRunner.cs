using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Compilation;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Tests.Common.Compilation.InstructionHandlers;

public class InstructionTestRunner
{
    private const string TestMethodName = "TestMethod";
    private readonly GameClass _gameClass;
    private readonly Assembly _assembly;

    public TestNesHal Hal { get; }

    public InstructionTestRunner(InstructionHandler handler, byte opcode, params byte[] operands)
    {
        var instruction = FormInstruction(opcode, operands);
        Hal = new TestNesHal();

        var assemblyBuilder = new PersistedAssemblyBuilder(new AssemblyName("test"), typeof(object).Assembly);
        _gameClass = SetupGameClass(assemblyBuilder, handler, instruction);
        _assembly = LoadAssembly(assemblyBuilder);
    }

    public void RunTestMethod()
    {
        var gameClassType = _assembly.GetType(_gameClass.Type.FullName!, true)!;
        var gameClassInstance = Activator.CreateInstance(gameClassType)!;
        var testMethod = gameClassInstance.GetType().GetMethod(TestMethodName);
        if (testMethod == null)
        {
            var message = $"No method found with the name '${TestMethodName}'";
            throw new InvalidOperationException(message);
        }

        // Add the HAL
        var field = gameClassInstance.GetType().GetField(_gameClass.HardwareField.Name);
        if (field == null)
        {
            throw new InvalidOperationException("HAL field not found on game class instance");
        }

        field.SetValue(gameClassInstance, Hal);

        testMethod.Invoke(null, []);
    }

    private static GameClass SetupGameClass(
        PersistedAssemblyBuilder assemblyBuilder,
        InstructionHandler handler, 
        DisassembledInstruction instruction)
    {
        var rootModule = assemblyBuilder.DefineDynamicModule("<Module>");
        var gameType = rootModule.DefineType("test.Game", TypeAttributes.Public);
        var hardwareField = gameType.DefineField(
            "Hardware",
            typeof(INesHal),
            FieldAttributes.Public | FieldAttributes.Static);

        var cpuRegistersField = new CpuRegisterClassBuilder("test", rootModule);

        var gameClass = new GameClass
        {
            Type = gameType,
            HardwareField = hardwareField,
            Registers = cpuRegistersField,
        };

        var methodBuilder = gameType.DefineMethod(TestMethodName, MethodAttributes.Public | MethodAttributes.Static);
        var ilGenerator = methodBuilder.GetILGenerator();

        handler.Handle(ilGenerator, instruction, gameClass);
        ilGenerator.Emit(OpCodes.Ret);

        gameType.CreateType();

        return gameClass;
    }

    private static Assembly LoadAssembly(PersistedAssemblyBuilder assemblyBuilder)
    {
        using var stream = new MemoryStream();
        assemblyBuilder.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return AssemblyLoadContext.Default.LoadFromStream(stream);
    }

    private static DisassembledInstruction FormInstruction(byte opcode, params byte[] operands)
    {
        var instructionInfo = InstructionSet.GetInstruction(opcode);
        if (!instructionInfo.IsValid)
        {
            var message = $"Tried to form instruction from invalid opcode: {opcode}";
            throw new InvalidOperationException(message);
        }

        return new DisassembledInstruction
        {
            Info = instructionInfo,
            Bytes = new[] { opcode }.Concat(operands).ToArray(),
        };
    }
}