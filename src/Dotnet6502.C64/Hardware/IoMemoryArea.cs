using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class IoMemoryArea : IMemoryDevice
{
    private const int TotalSize = 0xdfff - 0xd000 + 1;
    private readonly byte[] _data = new byte[TotalSize];

    public uint Size => TotalSize;

    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public Memory<byte> Vic2Registers => _data.AsMemory()[..0x400];
    public Memory<byte> SidRegisters => _data.AsMemory()[0x400..0x400];
    public Memory<byte> ColorRam => _data.AsMemory()[0x800..0x400];
    public Memory<byte> Cia1 => _data.AsMemory()[0xc00..0x100];
    public Memory<byte> Cia2 => _data.AsMemory()[0xd00..0x100];
    public Memory<byte> Io1 => _data.AsMemory()[0xe00..0x100];
    public Memory<byte> Io2 => _data.AsMemory()[0xf00..0x100];

    public void Write(ushort offset, byte value)
    {
        _data[offset] = value;
    }

    public byte Read(ushort offset)
    {
        return _data[offset];
    }
}