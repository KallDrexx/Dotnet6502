namespace Dotnet6502.Nes;

public class NesMemory
{
    private readonly Ppu _ppu;
    private enum MemoryType { InternalRam, Ppu, Rp2A03, CartridgeSpace }
    private readonly byte[] _internalRam = new byte[0x800]; // 2KB
    private readonly byte[] _cartridgeSpace = new byte[0x7FFF];

    public NesMemory(Ppu ppu)
    {
        _ppu = ppu;
        _ppu.CpuMemory = _internalRam;
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

            case MemoryType.CartridgeSpace:
                var offsetAddress = address - 0x7FFF;
                _cartridgeSpace[offsetAddress] = value;
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

            case MemoryType.CartridgeSpace:
                var offsetAddress = address - 0x7FFF;
                return _cartridgeSpace[offsetAddress];

            default:
                throw new NotSupportedException(memoryType.ToString());
        }
    }

    private static MemoryType GetMemoryType(ushort address)
    {
        return address switch
        {
            0x4014 => MemoryType.Ppu, // OAMDATA
            < 0x2000 => MemoryType.InternalRam,
            < 0x4000 => MemoryType.Ppu,
            < 0x4020 => MemoryType.Rp2A03,
            _ => MemoryType.CartridgeSpace,
        };
    }

    private static ushort NormalizeInternalRamAddress(ushort address)
    {
        // 0x0800 - 0x2000 mirrors the first 2k range, so normalize down to that.
        if ((address & 0x1000) > 0)
        {
            address = (ushort)(address & 0x0FFF);
        }

        if ((address & 0x0F00) > 0)
        {
            address = (ushort)(address - 0x800);
        }

        return address;
    }
}