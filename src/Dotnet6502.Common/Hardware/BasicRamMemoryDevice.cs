namespace Dotnet6502.Common.Hardware;

/// <summary>
/// A memory device that consists of a basic block of RAM
/// </summary>
public class BasicRamMemoryDevice : IMemoryDevice
{
    private readonly byte[] _bytes;

    public uint Size => (uint)_bytes.Length;

    public ReadOnlyMemory<byte>? RawBlockFromZero => _bytes.AsMemory();

    public BasicRamMemoryDevice(int size)
    {
        if (size is > ushort.MaxValue + 1 or <= 0)
        {
            var message = $"Size value of {size} is invalid for the basic ram memory device";
            throw new InvalidOperationException(message);
        }

        _bytes = new byte[size];
    }

    public void Write(ushort offset, byte value)
    {
        _bytes[offset] = value;
    }

    public byte Read(ushort offset)
    {
        return _bytes[offset];
    }
}