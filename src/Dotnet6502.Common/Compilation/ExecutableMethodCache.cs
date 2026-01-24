using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    // Rough guess at a value that can keep a full program worth of functions in memory without constant
    // eviction every frame.
    public const int MaxCachedMethodCount = 2000;

    /// <summary>
    /// Information about a cached executable method
    /// </summary>
    /// <param name="Method">The delegate to execute the method with</param>
    /// <param name="RelevantPages">Which memory pages are relevant to this method</param>
    /// <param name="LruEntry">The exact node in the LRU that refers to this method</param>
    /// <param name="InstructionAddresses">Every address relevant to the method</param>
    private record MethodInfo(
        ExecutableMethod Method,
        HashSet<byte> RelevantPages,
        LinkedListNode<ushort> LruEntry,
        HashSet<ushort> InstructionAddresses);

    private readonly Dictionary<ushort, MethodInfo> _executableMethods = new();

    /// <summary>
    /// Tracks what cached function addresses have instructions in each page
    /// </summary>
    private readonly Dictionary<byte, HashSet<ushort>> _pageToRelevantFunctionAddressMap = new();

    /// <summary>
    /// Contains a list of all cached function addresses, ordered by the most recently requested
    /// methods at the end.
    /// </summary>
    private readonly LinkedList<ushort> _lruCache = [];

    /// <summary>
    /// Memory addresses that have been written to that may require functions to be evicted from the cache
    /// at the next opportunity
    /// </summary>
    private readonly HashSet<ushort> _pendingAddressInvalidations = [];

    public void AddExecutableMethod(ExecutableMethod method, DecompiledFunction decompiledFunction)
    {
        var relevantPages = decompiledFunction.OrderedInstructions
            .Select(x => GetPageNumber(x.CPUAddress))
            .ToHashSet();

        var relevantAddresses = decompiledFunction.OrderedInstructions
            .Where(x => x.SubAddressOrder == 0) // only real instructions
            .SelectMany(x => Enumerable.Range(0, x.Info.Size).Select(y => (ushort)(x.CPUAddress + y)))
            .ToHashSet();

        var info = new MethodInfo(method, relevantPages, new LinkedListNode<ushort>(decompiledFunction.Address), relevantAddresses);
        _executableMethods[decompiledFunction.Address] = info;
        _lruCache.AddLast(info.LruEntry);

        foreach (var page in relevantPages)
        {
            if (!_pageToRelevantFunctionAddressMap.TryGetValue(page, out var list))
            {
                list = [];
                _pageToRelevantFunctionAddressMap.Add(page, list);
            }

            list.Add(decompiledFunction.Address);
        }

        while (_lruCache.Count > MaxCachedMethodCount)
        {
            var entry = _lruCache.First!; // Count guarantees we have at least one item
            RemoveCachedMethod(entry.Value);
        }
    }

    public ExecutableMethod? GetMethodForAddress(ushort functionStartAddress)
    {
        // Process pending memory page invalidations. Do this here instead of on memory changed notifications
        // As this should be much less frequent.
        ProcessPendingMemoryInvalidations();

        if (!_executableMethods.TryGetValue(functionStartAddress, out var info))
        {
            return null;
        }

        // Refresh the lru cache
        _lruCache.Remove(info.LruEntry);
        _lruCache.AddLast(info.LruEntry);

        return info.Method;
    }

    /// <summary>
    /// Checks if the specified address is part of an instruction from the specified function
    /// </summary>
    public bool AddressPartOfFunctionInstructions(ushort functionAddress, ushort checkAddress)
    {
        return _executableMethods.TryGetValue(functionAddress, out var info) &&
               info.InstructionAddresses.Contains(checkAddress);
    }

    /// <summary>
    /// Notifies the cache that the value located at the specified memory address has changed, and if any
    /// functions are cached at those relevant sections then they should be invalidated.
    /// </summary>
    public void MemoryChanged(ushort address)
    {
        _pendingAddressInvalidations.Add(address);
    }

    private static byte GetPageNumber(ushort address)
    {
        return (byte)(address >> 8);
    }

    private void ProcessPendingMemoryInvalidations()
    {
        var changesByPage = _pendingAddressInvalidations.GroupBy(GetPageNumber)
            .ToDictionary(x => x.Key, x => x.ToHashSet());

        foreach (var page in changesByPage)
        {
            if (!_pageToRelevantFunctionAddressMap.TryGetValue(page.Key, out var functionAddresses))
            {
                continue;
            }

            foreach (var functionAddress in functionAddresses)
            {
                var function = _executableMethods[functionAddress];
                if (function.InstructionAddresses.Any(x => page.Value.Contains(x)))
                {
                    RemoveCachedMethod(functionAddress);
                }
            }
        }

        _pendingAddressInvalidations.Clear();
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

        _lruCache.Remove(info.LruEntry);
    }
}