namespace Dotnet6502.Nes;

public class NesMemory
{
    private const int UnmappedSpaceSize = 0xBFE0;

    private readonly Ppu _ppu;
    private enum MemoryType { InternalRam, Ppu, Rp2A03, UnmappedSpace, PpuOamDma }
    private readonly byte[] _internalRam = new byte[0x800]; // 2KB
    private readonly byte[] _unmappedSpace = new byte[0x8000];

    public NesMemory(Ppu ppu, byte[] prgRomData)
    {
        _ppu = ppu;

        if (prgRomData.Length != 0x8000)
        {
            throw new InvalidOperationException("Unexpected prgRomData length");
        }

        for (var x = 0; x < 0x8000; x++)
        {
            _unmappedSpace[x] = prgRomData[x];
        }
    }

    public void Write(ushort address, byte value)
    {
        var memoryType = GetMemoryType(address);
        switch (memoryType)
        {
            case MemoryType.InternalRam:
                var ramAddress = NormalizeInternalRamAddress(address);
                _internalRam[ramAddress] = value;
                break;

            case MemoryType.Ppu:
                _ppu.ProcessMemoryWrite(address, value);
                break;

            case MemoryType.Rp2A03:
                throw new NotImplementedException();

            case MemoryType.UnmappedSpace:
                var offsetAddress = address - UnmappedSpaceSize;
                _unmappedSpace[offsetAddress] = value;
                break;

            case MemoryType.PpuOamDma:
                PerformOamDma(value);
                break;

            default:
                throw new NotSupportedException(memoryType.ToString());
        }
    }

    public byte Read(ushort address)
    {
        var memoryType = GetMemoryType(address);
        switch (memoryType)
        {
            case MemoryType.InternalRam:
                var ramAddress = NormalizeInternalRamAddress(address);
                return _internalRam[ramAddress];

            case MemoryType.Ppu:
                return _ppu.ProcessMemoryRead(address);

            case MemoryType.Rp2A03:
                throw new NotImplementedException();

            case MemoryType.UnmappedSpace:
                var offsetAddress = address - UnmappedSpaceSize;
                return _unmappedSpace[offsetAddress];

            default:
                throw new NotSupportedException(memoryType.ToString());
        }
    }

    private static MemoryType GetMemoryType(ushort address)
    {
        return address switch
        {
            0x4014 => MemoryType.PpuOamDma,
            < 0x2000 => MemoryType.InternalRam,
            < 0x4000 => MemoryType.Ppu,
            < 0x4020 => MemoryType.Rp2A03,
            _ => MemoryType.UnmappedSpace,
        };
    }

    private static ushort NormalizeInternalRamAddress(ushort address)
    {
        // 0x0800 - 0x2000 mirrors the first 2k range, so normalize down to that.
        while (address >= 0x800)
        {
            address -= 0x800;
        }

        return address;
    }

    private void PerformOamDma(byte page)
    {
        // Reset OAMADDR register to zero, then copy the 256 bytes of memory from the page and write
        // it to OAMDATA.
        _ppu.ProcessMemoryWrite(0x2003, 0);

        var address = (ushort)(page << 8);
        for (var x = 0; x <= 0xFF; x++)
        {
            var value = Read(address);
            _ppu.ProcessMemoryWrite(0x2004, value);
        }

        // In real hardware this would have taken 513 CPU cycles, so increment the PPU by that much
        _ppu.RunNextStep(513 / 3); // 3 PPU cycles = 1 CPU cycle
    }
}