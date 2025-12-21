using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    private readonly Dictionary<ushort, ExecutableMethod> _executableMethods = new();

    public void AddExecutableMethod(ExecutableMethod method, DecompiledFunction decompiledFunction)
    {
        _executableMethods[decompiledFunction.Address] = method;
    }

    public ExecutableMethod? GetMethodForAddress(ushort address)
    {
        return _executableMethods.GetValueOrDefault(address);
    }
}