using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// The PLA was a chip the C64 used to swap out RAM vs ROM vs I/O the memory mapping. This class emulates that behavior
/// by creating memory devices for each memory mappable section.
/// </summary>
public class ProgrammableLogicArray : IMemoryDevice
{
    private readonly byte[] _cpuIoPort = [0b00101111, 0b00110111];
    private readonly SwappableMemoryDevice _a000ToBfffDevice;
    private readonly SwappableMemoryDevice _d000ToDfffDevice;
    private readonly SwappableMemoryDevice _e000ToFfffDevice;

    public BasicRamMemoryDevice BasicRom { get; }
    public BasicRamMemoryDevice KernelRom { get; }
    public BasicRamMemoryDevice CharacterRom { get; }
    private readonly BasicRamMemoryDevice _ramA000ToBfff;
    private readonly BasicRamMemoryDevice _ramD000ToDfff;
    private readonly BasicRamMemoryDevice _ramE000ToFfff;
    private readonly IoMemoryArea _ioMemoryArea;

    public ProgrammableLogicArray(IoMemoryArea ioMemoryArea)
    {
        BasicRom = new BasicRamMemoryDevice(0xBFFF - 0xA000 + 1);
        KernelRom = new BasicRamMemoryDevice(0xFFFF - 0xE000 + 1);
        _ramA000ToBfff = new BasicRamMemoryDevice(0xBFFF - 0xA000 + 1);
        _ramD000ToDfff = new BasicRamMemoryDevice(0xDFFF - 0xD000 + 1);
        _ramE000ToFfff = new BasicRamMemoryDevice(0xFFFF - 0xE000 + 1);
        CharacterRom = new BasicRamMemoryDevice(0xDFFF - 0xD000 + 1);
        _ioMemoryArea = ioMemoryArea;

        _a000ToBfffDevice = new SwappableMemoryDevice(_ramA000ToBfff);
        _d000ToDfffDevice = new SwappableMemoryDevice(_ramD000ToDfff);
        _e000ToFfffDevice = new SwappableMemoryDevice(_ramE000ToFfff);

        UpdateDevices();
    }

    public void AttachToBus(MemoryBus memoryBus)
    {
        memoryBus.Attach(_a000ToBfffDevice, 0xa000);
        memoryBus.Attach(_d000ToDfffDevice, 0xd000);
        memoryBus.Attach(_e000ToFfffDevice, 0xe000);
    }

    private void UpdateDevices()
    {
        var section = _cpuIoPort[1] & 0b111;
        if ((section & 0b11) == 0)
        {
            // RAM visible in all 3 sections
            _a000ToBfffDevice.MakeVisible(_ramA000ToBfff, SwappableMemoryDevice.Mode.Read);
            _d000ToDfffDevice.MakeVisible(_ramD000ToDfff, SwappableMemoryDevice.Mode.Read);
            _e000ToFfffDevice.MakeVisible(_ramA000ToBfff, SwappableMemoryDevice.Mode.Read);

            return;
        }

        IMemoryDevice a0Device = (section & 0b011) == 0b11 ? BasicRom : _ramA000ToBfff;
        IMemoryDevice d0Device = (section & 0b100) > 0 ? _ioMemoryArea : CharacterRom;
        IMemoryDevice e0Device = (section & 0b011) == 0b01 ? _ramE000ToFfff : KernelRom;

        _a000ToBfffDevice.MakeVisible(a0Device, SwappableMemoryDevice.Mode.Read);
        _d000ToDfffDevice.MakeVisible(d0Device, SwappableMemoryDevice.Mode.Read);
        _e000ToFfffDevice.MakeVisible(e0Device, SwappableMemoryDevice.Mode.Read);
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