namespace Dotnet6502.Common.Hardware;

/// <summary>
/// A memory device that does nothing on writes and always reads 0
/// </summary>
public class NullMemoryDevice : IMemoryDevice
{
    public uint Size { get; }

    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public NullMemoryDevice(uint size)
    {
        Size = size;
    }

    public void Write(ushort offset, byte value)
    {
    }

    public byte Read(ushort offset)
    {
        return 0;
    }
}