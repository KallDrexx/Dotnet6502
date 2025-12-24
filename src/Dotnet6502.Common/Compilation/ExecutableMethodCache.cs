using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    private record MethodInfo(
        ExecutableMethod Method,
        DecompiledFunction DecompiledFunction,
        HashSet<byte> RelevantPages)
    {
        public bool IsInvalidated { get; set; }
    }

    private readonly Dictionary<ushort, MethodInfo> _executableMethods = new();

    /// <summary>
    /// Tracks what cached function addresses have instructions in each page
    /// </summary>
    private readonly Dictionary<byte, HashSet<ushort>> _pageToRelevantFunctionAddressMap = new();

    public void AddExecutableMethod(ExecutableMethod method, DecompiledFunction decompiledFunction)
    {
        var relevantPages = decompiledFunction.OrderedInstructions
            .Select(x => GetPageNumber(x.CPUAddress))
            .ToHashSet();

        var info = new MethodInfo(method, decompiledFunction, relevantPages);
        _executableMethods[decompiledFunction.Address] = info;

        foreach (var page in relevantPages)
        {
            if (!_pageToRelevantFunctionAddressMap.TryGetValue(page, out var list))
            {
                list = [];
                _pageToRelevantFunctionAddressMap.Add(page, list);
            }

            list.Add(decompiledFunction.Address);
        }
    }

    public ExecutableMethod? GetMethodForAddress(ushort functionStartAddress)
    {
        if (!_executableMethods.TryGetValue(functionStartAddress, out var info))
        {
            return null;
        }

        if (info.IsInvalidated)
        {
            // Method marked as invalidated, so remove references to it
            RemoveCachedMethod(functionStartAddress);

            return null;
        }

        return info.Method;
    }

    public void MemoryChanged(ushort address)
    {
        // To keep things simple and fast, invalidate all functions relevant to that page. This saves us
        // from having to track each specific memory address individually. This means that every memory change
        // up to 128 "other" instructions could be invalidated. However, that seems worth it to me as memory
        // updates will usually happen in consecutive addresses, and thus we don't want to have to repeat the
        // full invalidation process for each one individually. It may be necessary to sub-page this later but
        // we'll see.

        var page = GetPageNumber(address);
        if (_pageToRelevantFunctionAddressMap.TryGetValue(page, out var addresses))
        {
            foreach (var functionAddress in addresses)
            {
                _executableMethods[functionAddress].IsInvalidated = true;
            }

            // Remove them so we don't iterate the list next time.
            addresses.Clear();
        }
    }

    private static byte GetPageNumber(ushort address)
    {
        return (byte)(address >> 8);
    }

    private void RemoveCachedMethod(ushort address)
    {
        if (!_executableMethods.Remove(address, out var info))
        {
            return;
        }

        foreach (var page in info.RelevantPages)
        {
            var addresses = _pageToRelevantFunctionAddressMap[page]; // guaranteed to exist, might be empty
            addresses.Remove(address);
        }
    }
}