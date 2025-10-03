using Dotnet6502.Common;

namespace Dotnet6502.ComprehensiveTestRunner;

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
}
