using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class IoMemoryArea : IMemoryDevice
{
    public uint Size => 0xdfff - 0xd000 + 1;

    public ReadOnlyMemory<byte>? RawBlockFromZero { get; }

    public void Write(ushort offset, byte value)
    {
        throw new NotImplementedException();
    }

    public byte Read(ushort offset)
    {
        throw new NotImplementedException();
    }
}