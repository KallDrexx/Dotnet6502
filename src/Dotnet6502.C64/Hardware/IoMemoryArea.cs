using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class IoMemoryArea : IMemoryDevice
{
    private const int TotalSize = 0xdfff - 0xd000 + 1;

    private readonly MemoryBus _memoryBus;

    public uint Size => TotalSize;

    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public readonly BasicRamMemoryDevice Vic2Registers = new(64);
    public readonly BasicRamMemoryDevice SidRegisters = new(0x400);
    public readonly BasicRamMemoryDevice ColorRam = new(0x400);
    public readonly ComplexInterfaceAdapter Cia1 = new();
    public readonly ComplexInterfaceAdapter Cia2 = new();
    public readonly BasicRamMemoryDevice Io1 = new(0x100);
    public readonly BasicRamMemoryDevice Io2 = new(0x100);

    public IoMemoryArea()
    {
        _memoryBus = new MemoryBus(TotalSize);

        // Despite there only being 47 registers, the block of memory used is 64 bytes repeated until 0x400
        for (var x = 0; x < 10; x++)
        {
            _memoryBus.Attach(Vic2Registers, (ushort)(0x0000 + 0x40 * x));
        }

        _memoryBus.Attach(SidRegisters, 0x400);
        _memoryBus.Attach(ColorRam, 0x800);
        _memoryBus.Attach(Cia1, 0xc00);
        _memoryBus.Attach(Cia2, 0xd00);
        _memoryBus.Attach(Io1, 0xe00);
        _memoryBus.Attach(Io2, 0xf00);
    }

    public void Write(ushort offset, byte value)
    {
        _memoryBus.Write(offset, value);
    }

    public byte Read(ushort offset)
    {
        return _memoryBus.Read(offset);
    }
}