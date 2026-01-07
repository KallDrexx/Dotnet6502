using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Hardware;

/// <summary>
/// The memory bus that allows attaching memory devices at specific address spaces
/// </summary>
public class MemoryBus
{
    private record AttachedDevice(ushort BaseAddress, IMemoryDevice Device);

    private readonly List<AttachedDevice> _devices = [];

    /// <summary>
    /// Provides fast lookup of the index of the attached device mapped to a given region. Each index
    /// is incremented by 1, to allow a `0` to represent unmapped memory.
    /// </summary>
    private readonly ushort[] _deviceIndexMap;

    public MemoryBus(int memorySize)
    {
        _deviceIndexMap = new ushort[memorySize];
    }

    /// <summary>
    /// Attaches a memory device to the bus at a specific address
    /// </summary>
    /// <param name="device">The device to add</param>
    /// <param name="baseAddress">The base address this device is visible starting from</param>
    /// <param name="allowsOverriding">
    /// If true and the memory space is already occupied by another device, this device will take over
    /// responding to memory requests from the specified base address onto the size of the device being
    /// attached. This allows segmenting a portion of memory without needing to subdivide memory devices.
    ///
    /// If this is false, an exception will be thrown if any device has already claimed space between this
    /// device's base address and end address.
    /// </param>
    public void Attach(IMemoryDevice device, ushort baseAddress, bool allowsOverriding = false)
    {
        if (_devices.Count == ushort.MaxValue - 1)
        {
            throw new InvalidOperationException("Too many memory devices attached to bus");
        }

        // Make sure this doesn't overlap with an existing device
        if (!allowsOverriding)
        {
            var newDeviceEnd = baseAddress + device.Size;
            foreach (var (existingStart, memoryDevice) in _devices)
            {
                var existingEnd = existingStart + memoryDevice.Size;

                if (existingEnd > baseAddress && existingStart < newDeviceEnd)
                {
                    var message = $"Cannot attach device {device.GetType().Name} at address 0x{baseAddress:X4}-" +
                                  $"0x{newDeviceEnd:X4} as it overlaps with an already attached device of type " +
                                  $"{memoryDevice.GetType().Name} is using addresses 0x{existingStart:X4}-" +
                                  $"0x{existingEnd:X4}";

                    throw new InvalidOperationException(message);
                }
            }
        }

        // If we got here we can attach it.
        // Is this a new device or a respecification of an existing one?
        var deviceIndex = _devices.FindIndex(x => x.Device == device);
        if (deviceIndex < 0)
        {
            _devices.Add(new AttachedDevice(baseAddress, device));
            deviceIndex = (ushort)_devices.Count - 1;
        }

        // index is incremented so that 0 represents unmapped memory
        deviceIndex++;

        for (var x = 0; x < device.Size; x++)
        {
            _deviceIndexMap[baseAddress + x] = (ushort)deviceIndex;
        }
    }

    /// <summary>
    /// Writes a byte to the memory bus at the absolute address specified
    /// </summary>
    public void Write(ushort address, byte value)
    {
        var index = _deviceIndexMap[address];
        if (index == 0)
        {
            var message = $"No device mapped to address 0x{address:X4}";
            throw new InvalidOperationException(message);
        }

        var attachment = _devices[index - 1];
        var offset = address - attachment.BaseAddress;
        attachment.Device.Write((ushort)offset, value);
    }

    public byte Read(ushort address)
    {
        var index = _deviceIndexMap[address];
        if (index == 0)
        {
            var message = $"No device mapped to address 0x{address:X4}";
            throw new InvalidOperationException(message);
        }

        var attachment = _devices[index - 1];
        var offset = address - attachment.BaseAddress;
        return attachment.Device.Read((ushort)offset);
    }

    public IReadOnlyList<CodeRegion> GetAllCodeRegions()
    {
        return _devices.Select(x => new { x.BaseAddress, x.Device.RawBlockFromZero })
            .Where(x => x.RawBlockFromZero != null)
            .Select(x => new CodeRegion(x.BaseAddress, x.RawBlockFromZero!.Value))
            .ToArray();
    }
}