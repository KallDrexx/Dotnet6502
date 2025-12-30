using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    // Rough guess at a value that can keep a full program worth of functions in memory without constant
    // eviction every frame.
    public const int MaxCachedMethodCount = 2000;

    private record MethodInfo(ExecutableMethod Method, HashSet<byte> RelevantPages, LinkedListNode<ushort> LruEntry);

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
    /// Memory pages that have been written to and any functions that have instructions in that page should
    /// have their caches removed at the next opportunity.
    /// </summary>
    private readonly HashSet<byte> _pendingPageInvalidations = [];

    public void AddExecutableMethod(ExecutableMethod method, DecompiledFunction decompiledFunction)
    {
        var relevantPages = decompiledFunction.OrderedInstructions
            .Select(x => GetPageNumber(x.CPUAddress))
            .ToHashSet();

        var info = new MethodInfo(method, relevantPages, new LinkedListNode<ushort>(decompiledFunction.Address));
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
        foreach (var page in _pendingPageInvalidations)
        {
            InvalidateFunctionsAtPage(page);
        }

        _pendingPageInvalidations.Clear();

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
    /// Notifies the cache that the value located at the specified memory address has changed, and if any
    /// functions are cached at those relevant sections then they should be invalidated.
    /// </summary>
    public void MemoryChanged(ushort address)
    {
        var page = GetPageNumber(address);
        _pendingPageInvalidations.Add(page);
    }

    /// <summary>
    /// Notifies the cache that the values located at the specified memory address range has changed, and if any
    /// functions are cached at those relevant sections then they should be invalidated.
    /// </summary>
    public void BulkMemoryChanged(ushort startAddress, ushort lastAddress)
    {
        var firstPage = GetPageNumber(startAddress);
        var lastPage = GetPageNumber(lastAddress);
        foreach (byte page in Enumerable.Range(firstPage, lastPage - firstPage + 1))
        {
            _pendingPageInvalidations.Add(page);
        }
    }

    private static byte GetPageNumber(ushort address)
    {
        return (byte)(address >> 8);
    }

    private void InvalidateFunctionsAtPage(byte page)
    {
        if (_pageToRelevantFunctionAddressMap.TryGetValue(page, out var addresses))
        {
            foreach (var functionAddress in addresses)
            {
                RemoveCachedMethod(functionAddress);
            }

            addresses.Clear();
        }
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