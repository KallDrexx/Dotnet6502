using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// A memory mapped device that can be swapped out to different memory devices
/// </summary>
public class BankableMemoryDevice : IMemoryDevice
{
    private IMemoryDevice _currentMemoryMappedDevice;

    public uint Size => _currentMemoryMappedDevice.Size;

    public ReadOnlyMemory<byte>? RawBlockFromZero => _currentMemoryMappedDevice.RawBlockFromZero;

    public BankableMemoryDevice(IMemoryDevice initialMappedDevice)
    {
        _currentMemoryMappedDevice = initialMappedDevice;
    }

    public void SwapTo(IMemoryDevice memoryDevice)
    {
        if (memoryDevice.Size != _currentMemoryMappedDevice.Size)
        {
            var message = $"Attempted to swap memory device from one with {_currentMemoryMappedDevice.Size} " +
                          $"to one with {memoryDevice.Size}. Sizes must match";

            throw new ArgumentException(message);
        }

        _currentMemoryMappedDevice = memoryDevice;
    }

    public void Write(ushort offset, byte value)
    {
        _currentMemoryMappedDevice.Write(offset, value);
    }

    public byte Read(ushort offset)
    {
        return _currentMemoryMappedDevice.Read(offset);
    }
}