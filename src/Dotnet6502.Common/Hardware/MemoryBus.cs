using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Hardware;

/// <summary>
/// The memory bus that allows attaching memory devices at specific address spaces
/// </summary>
public class MemoryBus
{
    private record AttachedDevice(ushort BaseAddress, IMemoryDevice Device);
    private record VisibleRange(ushort DeviceIndex, int StartAddress, int EndAddress);

    private readonly List<AttachedDevice> _devices = [];

    /// <summary>
    /// Provides fast lookup of the index of the attached device mapped to a given region. Each index
    /// is incremented by 1, to allow a `0` to represent unmapped memory.
    /// </summary>
    private readonly ushort[] _deviceIndexMap;

    /// <summary>
    /// Cached list of visible memory ranges (which device is visible at which addresses).
    /// </summary>
    private List<VisibleRange> _cachedVisibleRanges = [];

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

        // Rebuild visible ranges cache to reflect the new memory mapping
        RebuildVisibleRangesCache();
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

    /// <summary>
    /// Rebuilds the cached visible ranges by scanning the device index map to find
    /// which device is visible at which addresses, accounting for overrides and fragmentation.
    /// </summary>
    private void RebuildVisibleRangesCache()
    {
        var visibleRanges = new List<VisibleRange>();
        var currentStart = -1;
        ushort currentDeviceIndex = 0;

        // Scan _deviceIndexMap to find continuous visible ranges
        for (var addr = 0; addr < _deviceIndexMap.Length; addr++)
        {
            var deviceIndex = _deviceIndexMap[addr];

            if (deviceIndex != currentDeviceIndex)
            {
                // Range boundary - save previous range if it was mapped
                if (currentStart >= 0 && currentDeviceIndex > 0)
                {
                    SplitAndAddRanges(visibleRanges, currentDeviceIndex, currentStart, addr);
                }

                // Start new range (or mark as unmapped if deviceIndex is 0)
                currentStart = deviceIndex > 0 ? addr : -1;
                currentDeviceIndex = deviceIndex;
            }
        }

        // Don't forget the last range if it extends to the end of memory
        if (currentStart >= 0 && currentDeviceIndex > 0)
        {
            SplitAndAddRanges(visibleRanges, currentDeviceIndex, currentStart, _deviceIndexMap.Length);
        }

        _cachedVisibleRanges = visibleRanges;
    }

    /// <summary>
    /// Splits a range into multiple ranges if it exceeds the device size (for mirrored devices).
    /// </summary>
    private void SplitAndAddRanges(List<VisibleRange> ranges, ushort deviceIndex, int startAddress, int endAddress)
    {
        var device = _devices[deviceIndex - 1].Device;
        var rangeLength = endAddress - startAddress;

        // If the range is larger than the device size, split it into multiple ranges
        // This handles mirrored devices (same device attached at multiple addresses)
        if (rangeLength > device.Size)
        {
            var currentAddr = startAddress;
            while (currentAddr < endAddress)
            {
                var chunkEnd = (int)Math.Min(currentAddr + device.Size, endAddress);
                ranges.Add(new VisibleRange(
                    DeviceIndex: deviceIndex,
                    StartAddress: currentAddr,
                    EndAddress: chunkEnd
                ));
                currentAddr = chunkEnd;
            }
        }
        else
        {
            // Normal range (not mirrored or fragmented within device bounds)
            ranges.Add(new VisibleRange(
                DeviceIndex: deviceIndex,
                StartAddress: startAddress,
                EndAddress: endAddress
            ));
        }
    }

    public IReadOnlyList<CodeRegion> GetAllCodeRegions()
    {
        var regions = new List<CodeRegion>();

        // Generate CodeRegions from cached visible ranges using current memory content
        foreach (var range in _cachedVisibleRanges)
        {
            var attachment = _devices[range.DeviceIndex - 1];
            var device = attachment.Device;
            var baseAddress = attachment.BaseAddress;

            // Skip devices without raw memory blocks (like IoMemoryArea, RoutableMemoryDevice)
            if (device.RawBlockFromZero == null)
                continue;

            var rawBlock = device.RawBlockFromZero.Value;

            // Calculate the offset into the device's memory
            // Use modulo to handle mirrored devices (same device attached at multiple addresses)
            var offsetInDevice = (uint)(range.StartAddress - baseAddress);
            var deviceOffsetStart = offsetInDevice % device.Size;
            var length = range.EndAddress - range.StartAddress;

            // Validate before slicing
            if (deviceOffsetStart + length > device.Size)
            {
                throw new InvalidOperationException(
                    $"Invalid slice calculation: offset={deviceOffsetStart}, length={length}, device.Size={device.Size}, " +
                    $"range={range.StartAddress:X4}-{range.EndAddress:X4}, baseAddress={baseAddress:X4}");
            }

            if (deviceOffsetStart + length > rawBlock.Length)
            {
                throw new InvalidOperationException(
                    $"Slice would exceed rawBlock: offset={deviceOffsetStart}, length={length}, rawBlock.Length={rawBlock.Length}, " +
                    $"device.Size={device.Size}, range={range.StartAddress:X4}-{range.EndAddress:X4}, baseAddress={baseAddress:X4}");
            }

            // Slice the device's memory to get the current visible portion
            var slicedMemory = rawBlock.Slice((int)deviceOffsetStart, length);

            regions.Add(new CodeRegion((ushort)range.StartAddress, slicedMemory));
        }

        return regions;
    }
}