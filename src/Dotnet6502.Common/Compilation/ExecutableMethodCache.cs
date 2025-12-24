using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

public class ExecutableMethodCache
{
    // Rough guess at a value that can keep a full program worth of functions in memory without constant
    // eviction every frame.
    public const int MaxCachedMethodCount = 2000;

    private record MethodInfo(ExecutableMethod Method, HashSet<byte> RelevantPages, LinkedListNode<ushort> LruEntry)
    {
        public bool IsInvalidated { get; set; }
    }

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

        // Refresh the lru cache
        _lruCache.Remove(info.LruEntry);
        _lruCache.AddLast(info.LruEntry);

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

        _lruCache.Remove(info.LruEntry);
    }
}