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
    private readonly ushort[] _deviceIndexMap = new ushort[0x10000];

    public void Attach(IMemoryDevice device, ushort baseAddress)
    {
        if (_devices.Count == ushort.MaxValue - 1)
        {
            throw new InvalidOperationException("Too many memory devices attached to bus");
        }

        // Make sure this doesn't overlap with an existing device
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

        // If we got here we can attach it
        _devices.Add(new AttachedDevice(baseAddress, device));

        var index = (ushort)_devices.Count; // index is incremented so that 0 represents unmapped memory
        for (var x = 0; x < device.Size; x++)
        {
            _deviceIndexMap[baseAddress + x] = index;
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