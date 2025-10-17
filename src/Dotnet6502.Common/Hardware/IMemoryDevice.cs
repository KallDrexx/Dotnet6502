namespace Dotnet6502.Common.Hardware;

/// <summary>
/// Represents a device that can have memory read or written to
/// </summary>
public interface IMemoryDevice
{
    /// <summary>
    /// How many bytes in size this device is valid for
    /// </summary>
    public uint Size { get; }

    /// <summary>
    /// Gets a raw block of memory from the zero offset address. If this device does not contain a block
    /// of memory (e.g. memory mapped registers) than `null` is returned.
    ///
    /// This is mostly used to retrieve regions of memory that could contain code to be disassembled.
    /// </summary>
    /// <returns></returns>
    ReadOnlyMemory<byte>? RawBlockFromZero { get; }

    /// <summary>
    /// Writes a byte to the device based on the offset from where the device is mapped to on the bus
    /// </summary>
    void Write(ushort offset, byte value);

    /// <summary>
    /// Reads a byte from the device based on the offset from where the device is mapped to on the bus
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    byte Read(ushort offset);
}