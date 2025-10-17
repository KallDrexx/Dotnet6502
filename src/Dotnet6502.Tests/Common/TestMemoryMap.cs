using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Tests.Common;

public class TestMemoryMap : IMemoryDevice
{
    public byte[] MemoryBlock { get; } = new byte[1024 * 64];

    public uint Size => (uint)MemoryBlock.Length;

    public ReadOnlyMemory<byte>? RawBlockFromZero => MemoryBlock.AsMemory();

    public byte Read(ushort address)
    {
        return MemoryBlock[address];
    }

    public void Write(ushort address, byte value)
    {
        MemoryBlock[address] = value;
    }
}