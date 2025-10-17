using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.ComprehensiveTestRunner;

public class TestMemoryMap : IMemoryDevice
{
    public byte[] MemoryBlock { get; } = new byte[1024 * 64];
    public List<ushort> ReadMemoryBlocks { get; } = [];
    public List<ushort> WrittenMemoryBlocks { get; } = [];

    public byte Read(ushort address)
    {
        ReadMemoryBlocks.Add(address);
        return MemoryBlock[address];
    }

    public uint Size => (uint)MemoryBlock.Length;

    public ReadOnlyMemory<byte>? RawBlockFromZero { get; }

    public void Write(ushort address, byte value)
    {
        WrittenMemoryBlocks.Add(address);
        MemoryBlock[address] = value;
    }

    public IReadOnlyList<CodeRegion> GetCodeRegions()
    {
        return [new CodeRegion(0, MemoryBlock)];
    }
}
