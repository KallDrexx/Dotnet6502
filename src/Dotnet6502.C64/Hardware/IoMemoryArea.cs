using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class IoMemoryArea : IMemoryDevice
{
    private readonly byte[] _data = new byte[0xdfff - 0xd000 + 1];

    public uint Size => 0xdfff - 0xd000 + 1;

    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public void Write(ushort offset, byte value)
    {
        _data[offset] = value;
    }

    public byte Read(ushort offset)
    {
        return _data[offset];
    }
}