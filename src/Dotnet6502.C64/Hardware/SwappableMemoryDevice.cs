using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// A memory mapped device that can be swapped out to different memory devices.
/// </summary>
public class SwappableMemoryDevice : IMemoryDevice
{
    /// <summary>
    /// The mode a memory device should be made visible with
    /// </summary>
    public enum Mode { Read, Write, ReadAndWrite }

    private IMemoryDevice _readDevice;
    private IMemoryDevice _writeDevice;

    public uint Size => _readDevice.Size;

    public ReadOnlyMemory<byte>? RawBlockFromZero => _readDevice.RawBlockFromZero;

    public SwappableMemoryDevice(IMemoryDevice readWriteDevice)
    {
        _readDevice = readWriteDevice;
        _writeDevice = readWriteDevice;
    }

    /// <summary>
    /// Makes the specified device responsible for read or write calls based on the mode. The modes not specified
    /// will retain their routes to the previous devices.
    /// </summary>
    public void MakeVisible(IMemoryDevice memoryDevice, Mode mode)
    {
        if (memoryDevice.Size != _readDevice.Size)
        {
            var message = $"Attempted to swap memory device from one with {_readDevice.Size} " +
                          $"to one with {memoryDevice.Size}. Sizes must match";

            throw new ArgumentException(message);
        }

        switch (mode)
        {
            case Mode.ReadAndWrite:
                _readDevice = memoryDevice;
                _writeDevice = memoryDevice;
                break;

            case Mode.Read:
                _readDevice = memoryDevice;
                break;

            case Mode.Write:
                _writeDevice = memoryDevice;
                break;

            default:
                throw new NotSupportedException(mode.ToString());
        }
    }

    public void Write(ushort offset, byte value)
    {
        _writeDevice.Write(offset, value);
    }

    public byte Read(ushort offset)
    {
        return _readDevice.Read(offset);
    }
}