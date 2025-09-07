using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.Decompilation;

namespace DotNetJit.Cli.Builder;

public class NesAssemblyBuilder
{
    private readonly string _rootNamespace;
    private readonly PersistedAssemblyBuilder _builder;

    public HardwareBuilder Hardware { get; }
    public VariableTrackerBuilder VariableTracker { get; }

    public NesAssemblyBuilder(string namespaceName, Decompiler decompiler)
    {
        _rootNamespace = namespaceName;

        _builder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _builder.DefineDynamicModule("<Module>");
        Hardware = new HardwareBuilder(_rootNamespace, rootModule);
        VariableTracker = new VariableTrackerBuilder(_rootNamespace, rootModule, decompiler.Variables);
    }

    public void Save(Stream outputStream)
    {
        _builder.Save(outputStream);
    }
}