using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Nes;

public class OamDmaDevice : IMemoryDevice
{
    private readonly Ppu _ppu;
    private readonly MemoryBus _readMemoryBus;

    public uint Size => 1;

    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public OamDmaDevice(Ppu ppu, MemoryBus readMemoryBus)
    {
        _ppu = ppu;
        _readMemoryBus = readMemoryBus;
    }

    public void Write(ushort offset, byte value)
    {
        // Reset OAMADDR register to zero, then copy the 256 bytes of memory from the page and write
        // it to OAMDATA.
        _ppu.Write(0x2003, 0);

        // Value received is the page to read from
        var address = (ushort)(value << 8);
        for (var x = 0; x <= 0xFF; x++)
        {
            var readValue = _readMemoryBus.Read((ushort)(address | x));
            _ppu.Write(0x2004, readValue);
        }

        // In real hardware this would have taken 513 CPU cycles, so increment the PPU by that much
        _ppu.RunNextStep(513); // 3 PPU cycles = 1 CPU cycle
    }

    public byte Read(ushort offset)
    {
        throw new NotImplementedException();
    }
}