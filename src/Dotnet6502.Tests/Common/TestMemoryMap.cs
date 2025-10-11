using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Tests.Common;

public class TestMemoryMap : IMemoryMap
{
    public byte[] MemoryBlock { get; } = new byte[1024 * 64];
    public List<ushort> ReadMemoryBlocks { get; } = [];

    public byte Read(ushort address)
    {
        ReadMemoryBlocks.Add(address);
        return MemoryBlock[address];
    }

    public void Write(ushort address, byte value)
    {
        MemoryBlock[address] = value;
    }

    public IReadOnlyList<CodeRegion> GetCodeRegions()
    {
        return [new CodeRegion(0, MemoryBlock)];
    }
}