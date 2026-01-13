using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// A memory device that's routable between different memory devices
/// </summary>
public class RoutableMemoryDevice : IMemoryDevice
{
    public enum RoutableDirection { Read, Write, ReadAndWrite }

    private readonly Dictionary<IMemoryDevice, ushort> _deviceToOffsetAdjustmentMap = new();
    private IMemoryDevice? _routableDeviceForRead;
    private IMemoryDevice? _routableDeviceForWrite;

    public uint Size { get; }
    public ReadOnlyMemory<byte>? RawBlockFromZero => _routableDeviceForRead!.RawBlockFromZero;

    public RoutableMemoryDevice(uint size)
    {
        Size = size;
    }

    public void Write(ushort offset, byte value)
    {
        if (_routableDeviceForWrite == null)
        {
            const string message = "Attempted to write to a device, but no device has been set as the routable target";
            throw new InvalidOperationException(message);
        }

        var adjustment = _deviceToOffsetAdjustmentMap[_routableDeviceForWrite];
        _routableDeviceForWrite.Write((ushort)(offset + adjustment), value);
    }

    public byte Read(ushort offset)
    {
        if (_routableDeviceForRead == null)
        {
            const string message = "Attempted to read from a device, but no device has been set as the routable target";
            throw new InvalidOperationException(message);
        }

        var adjustment = _deviceToOffsetAdjustmentMap[_routableDeviceForRead];
        return _routableDeviceForRead.Read((ushort)(offset + adjustment));
    }

    /// <summary>
    /// Adds a device that can be routed to, (but is not yet routed). The device must be at least the size
    /// of this RoutableMemoryDevice from the offset adjustment to the end.
    /// </summary>
    public void Add(IMemoryDevice memoryDevice, ushort offsetAdjustment)
    {
        var deviceSize = memoryDevice.Size - offsetAdjustment;
        if (deviceSize < Size)
        {
            var message = $"Attempted to add a memory device with a size of {memoryDevice.Size:X4} at an offset " +
                          $"adjustment of {offsetAdjustment:X4}, which ends up with a visible size of {deviceSize:X4}. " +
                          $"This is less than this routable memory device's size of {Size}";
            throw new ArgumentException(message);
        }

        _deviceToOffsetAdjustmentMap.Add(memoryDevice, offsetAdjustment);
    }

    /// <summary>
    /// Sets the device with the specified id as routable in the specified direction
    /// </summary>
    public void SetRoutableDevice(IMemoryDevice device, RoutableDirection direction)
    {
        if (!_deviceToOffsetAdjustmentMap.ContainsKey(device))
        {
            const string message = "Attempted to set routable device with a device that has not been added yet";
            throw new ArgumentException(message);
        }

        switch (direction)
        {
            case RoutableDirection.Read:
                _routableDeviceForRead = device;
                break;

            case RoutableDirection.Write:
                _routableDeviceForWrite = device;
                break;

            case RoutableDirection.ReadAndWrite:
                _routableDeviceForRead = device;
                _routableDeviceForWrite = device;
                break;

            default:
                throw new NotSupportedException(direction.ToString());
        }
    }
}