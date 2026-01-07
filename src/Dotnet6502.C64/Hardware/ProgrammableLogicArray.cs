using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// The PLA was a chip the C64 used to swap out RAM vs ROM vs I/O the memory mapping. This class emulates that behavior
/// by creating memory devices for each memory mappable section.
/// </summary>
public class ProgrammableLogicArray : IMemoryDevice
{
    private readonly byte[] _cpuIoPort = [0b00101111, 0b00110111];
    private readonly C64MemoryConfig _memoryConfig;

    private readonly RoutableMemoryDevice _a000ToBfffDevice = new(0xBFFF - 0xA000 + 1);
    private readonly RoutableMemoryDevice _d000ToDfffDevice = new(0xDFFF - 0xD000 + 1);
    private readonly RoutableMemoryDevice _e000ToFfffDevice = new(0xFFFF - 0xE000 + 1);

    public ProgrammableLogicArray(C64MemoryConfig memoryConfig)
    {
        _memoryConfig = memoryConfig;

        _a000ToBfffDevice.Add(memoryConfig.FullRam, 0xA000);
        _a000ToBfffDevice.Add(memoryConfig.BasicRom, 0x0000);

        _d000ToDfffDevice.Add(memoryConfig.FullRam, 0xD000);
        _d000ToDfffDevice.Add(memoryConfig.IoMemoryArea, 0x0000);
        _d000ToDfffDevice.Add(memoryConfig.CharRom, 0x0000);

        _e000ToFfffDevice.Add(memoryConfig.FullRam, 0xE000);
        _e000ToFfffDevice.Add(memoryConfig.KernelRom, 0x0000);

        _memoryConfig.CpuMemoryBus.Attach(this, 0x0000, true);
        _memoryConfig.CpuMemoryBus.Attach(_a000ToBfffDevice, 0xa000, true);
        _memoryConfig.CpuMemoryBus.Attach(_d000ToDfffDevice, 0xd000, true);
        _memoryConfig.CpuMemoryBus.Attach(_e000ToFfffDevice, 0xe000, true);

        UpdateDevices();
    }

    private void UpdateDevices()
    {
        var section = _cpuIoPort[1] & 0b111;
        if ((section & 0b11) == 0)
        {
            // RAM visible in all 3 sections
            _a000ToBfffDevice.SetRoutableDevice(_memoryConfig.FullRam, RoutableMemoryDevice.RoutableDirection.Read);
            _d000ToDfffDevice.SetRoutableDevice(_memoryConfig.FullRam, RoutableMemoryDevice.RoutableDirection.Read);
            _e000ToFfffDevice.SetRoutableDevice(_memoryConfig.FullRam, RoutableMemoryDevice.RoutableDirection.Read);

            return;
        }

        IMemoryDevice a0Device = (section & 0b011) == 0b11 ? _memoryConfig.BasicRom : _memoryConfig.FullRam;
        IMemoryDevice d0Device = (section & 0b100) > 0 ? _memoryConfig.IoMemoryArea : _memoryConfig.CharRom;
        IMemoryDevice e0Device = (section & 0b011) == 0b01 ? _memoryConfig.FullRam : _memoryConfig.KernelRom;

        _a000ToBfffDevice.SetRoutableDevice(a0Device, RoutableMemoryDevice.RoutableDirection.Read);
        _d000ToDfffDevice.SetRoutableDevice(d0Device, RoutableMemoryDevice.RoutableDirection.Read);
        _e000ToFfffDevice.SetRoutableDevice(e0Device, RoutableMemoryDevice.RoutableDirection.Read);
    }

    public uint Size => (uint) _cpuIoPort.Length;

    public ReadOnlyMemory<byte>? RawBlockFromZero => _cpuIoPort.AsMemory();

    public void Write(ushort offset, byte value)
    {
        // TODO: Add port direction functionality
        _cpuIoPort[offset] = value;
        if (offset == 1)
        {
            UpdateDevices();
        }
    }

    public byte Read(ushort offset)
    {
        return _cpuIoPort[offset];
    }
}