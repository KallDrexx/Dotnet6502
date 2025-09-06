using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class NesAssemblyBuilder
{
    private readonly string _rootNamespace;
    private readonly PersistedAssemblyBuilder _builder;

    public HardwareBuilder Hardwares { get; }

    public NesAssemblyBuilder(string namespaceName)
    {
        _rootNamespace = namespaceName;
        Hardwares = new HardwareBuilder(_rootNamespace);

        _builder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _builder.DefineDynamicModule("<Module>");
        Hardwares.AddCpuRegisterType(rootModule);
    }

    public void Save(Stream outputStream)
    {
        _builder.Save(outputStream);
    }
}