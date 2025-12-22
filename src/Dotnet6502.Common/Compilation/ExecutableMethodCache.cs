using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    private record MethodInfo(ExecutableMethod Method, DecompiledFunction DecompiledFunction);

    private readonly Dictionary<ushort, MethodInfo> _executableMethods = new();

    /// <summary>
    /// Stores each cached function which contains instructions for each memory address. This allows
    /// for fast lookups for cache invalidation due to a memory address being updated (and thus the instruction
    /// may have changed).
    ///
    /// Should provide fast lookups with reasonable memory tradeoffs, since we can only have 65k total values.
    /// </summary>
    private readonly SortedList<ushort, List<ushort>> _memoryAddressesToFunctionMap = new();

    public void AddExecutableMethod(ExecutableMethod method, DecompiledFunction decompiledFunction)
    {
        var info = new MethodInfo(method, decompiledFunction);
        _executableMethods[decompiledFunction.Address] = info;

        foreach (var instruction in decompiledFunction.OrderedInstructions)
        {
            for (var x = 0; x < instruction.Info.Size; x++)
            {
                var address = (ushort)(instruction.CPUAddress + x);
                if (!_memoryAddressesToFunctionMap.TryGetValue(address, out var list))
                {
                    list = [];
                    _memoryAddressesToFunctionMap.Add(address, list);
                }

                list.Add(decompiledFunction.Address);
            }
        }
    }

    public ExecutableMethod? GetMethodForAddress(ushort functionStartAddress)
    {
        return _executableMethods.GetValueOrDefault(functionStartAddress)?.Method;
    }

    public void MemoryChanged(ushort address)
    {
        // This should be a quick lookup for irrelevant memory addresses.
        if (!_memoryAddressesToFunctionMap.TryGetValue(address, out var relevantFunctionAddresses))
        {
            // Memory not marked as relevant to a cached method
            return;
        }

        // We have to perform the invalidations at some point, so might as well do it here. It's possible
        // we could speed the process up by just marking the function as invalidated and remove it
        // some other time, but at some point we do need to iterate through all the addresses and remove it. Not
        // sure any spot is better than any others atm.
        foreach (var relevantFunctionAddress in relevantFunctionAddresses)
        {
            if (!_executableMethods.Remove(relevantFunctionAddress, out var info))
            {
                continue;
            }

            foreach (var instruction in info.DecompiledFunction.OrderedInstructions)
            {
                for (var x = 0; x < instruction.Info.Size; x++)
                {
                    var address = (ushort)(instruction.CPUAddress + instruction.Info.Size);

                }
            }
        }
    }
}