using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// The PLA was a chip the C64 used to swap out RAM vs ROM vs I/O the memory mapping. This class emulates that behavior
/// by creating memory devices for each memory mappable section.
/// </summary>
public class ProgrammableLogicArray : IMemoryDevice
{
    private readonly byte[] _cpuIoPort = [0b00101111, 0b00110111];
    private BankableMemoryDevice _a000ToBfffDevice;
    private BankableMemoryDevice _d000ToDfffDevice;
    private BankableMemoryDevice _e000ToFfffDevice;

    private readonly BasicRamMemoryDevice _basicRom;
    private readonly BasicRamMemoryDevice _kernelRom;
    private readonly BasicRamMemoryDevice _ramA000ToBfff;
    private readonly BasicRamMemoryDevice _ramD000ToDfff;
    private readonly BasicRamMemoryDevice _ramE000ToFfff;
    private readonly BasicRamMemoryDevice _characterRom;
    private readonly IoMemoryArea _ioMemoryArea;

    public ProgrammableLogicArray()
    {
        _basicRom = new BasicRamMemoryDevice(0xBFFF - 0xA000 + 1);
        _kernelRom = new BasicRamMemoryDevice(0xFFFF - 0xE000 + 1);
        _ramA000ToBfff = new BasicRamMemoryDevice(0xBFFF - 0xA000 + 1);
        _ramD000ToDfff = new BasicRamMemoryDevice(0xDFFF - 0xD000 + 1);
        _ramE000ToFfff = new BasicRamMemoryDevice(0xFFFF - 0xE000 + 1);
        _characterRom = new BasicRamMemoryDevice(0xDFFF - 0xD000 + 1);
        _ioMemoryArea = new IoMemoryArea();

        _a000ToBfffDevice = new BankableMemoryDevice(_ramA000ToBfff);
        _d000ToDfffDevice = new BankableMemoryDevice(_ramD000ToDfff);
        _e000ToFfffDevice = new BankableMemoryDevice(_ramE000ToFfff);

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
            _a000ToBfffDevice.SwapTo(_ramA000ToBfff);
            _d000ToDfffDevice.SwapTo(_ramD000ToDfff);
            _e000ToFfffDevice.SwapTo(_ramE000ToFfff);

            return;
        }

        _d000ToDfffDevice.SwapTo((section & 0b100) > 0
            ? _ioMemoryArea
            : _characterRom);

        _e000ToFfffDevice.SwapTo((section & 0b011) == 0b01
            ? _ramE000ToFfff
            : _kernelRom);

        _a000ToBfffDevice.SwapTo((section & 0b011) == 0b11
            ? _basicRom
            : _ramA000ToBfff);
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