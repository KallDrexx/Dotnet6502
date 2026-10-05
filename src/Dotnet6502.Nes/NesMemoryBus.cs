using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;
using System;

namespace Dotnet6502.Nes;

public class NesMemoryBus : IMemoryBus
{
    private readonly BasicRamMemoryDevice _cpuRam = new(0x800);
    private readonly BasicRamMemoryDevice _cartridgeSpace = new(0xBFE0);
    private readonly Joystick1 _joystick1;
    private readonly OamDmaDevice _oamDmaDevice;
    private readonly Ppu _ppu;
    private readonly IReadOnlyList<CodeRegion> _codeRegions;

    public NesMemoryBus(Ppu ppu, INesInput inputDevice, byte[] cartridgeBytes)
    {
        _joystick1 = new Joystick1(inputDevice);
        _ppu = ppu;
        _oamDmaDevice = new OamDmaDevice(ppu, this);

        if (cartridgeBytes.Length % 0x4000 != 0)
        {
            var message = $"Expected prgRom as multiple of 0x4000, instead it was 0x{cartridgeBytes.Length:X4}";
            throw new InvalidOperationException(message);
        }

        for (var x = 0; x < cartridgeBytes.Length; x++)
        {
            var unmappedSpaceIndex = _cartridgeSpace.Size - x - 1;
            var prgRomDataIndex = cartridgeBytes.Length - x - 1;
            _cartridgeSpace.Write((ushort)unmappedSpaceIndex, cartridgeBytes[prgRomDataIndex]);
        }

        _codeRegions = [
            new CodeRegion(0x0000, _cpuRam.RawBlockFromZero!.Value),
            new CodeRegion(0x0800, _cpuRam.RawBlockFromZero!.Value),
            new CodeRegion(0x1000, _cpuRam.RawBlockFromZero!.Value),
            new CodeRegion(0x1800, _cpuRam.RawBlockFromZero!.Value),
            new CodeRegion(0x4020, _cartridgeSpace.RawBlockFromZero!.Value),
        ];
    }

    public IReadOnlyList<CodeRegion> GetAllCodeRegions()
    {
        return _codeRegions;
    }

    public byte Read(ushort address)
    {
        if (address < 0x2000)
        {
            // First 2KB is repeated 4 times
            return _cpuRam.Read((ushort)(address % 0x800));
        }

        if (address < 0x4000)
        {
            // PPU register, repeats every 8bytes
            var offset = (ushort)((address - 0x2000) % 8);
            return _ppu.Read(offset);
        }

        if (address == 0x4014)
        {
            return _oamDmaDevice.Read(0);
        }

        if (address == 0x4016)
        {
            return _joystick1.Read(0);
        }

        if (address < 0x4020)
        {
            // APU and disabled I/O. Always disabled
            return 0;
        }

        // Remainding is the cartridge space
        var cartridgeOffset = (ushort)(address - 0x4020);
        return _cartridgeSpace.Read(cartridgeOffset);
    }

    public void Write(ushort address, byte value)
    {
        if (address < 0x2000)
        {
            // First 2KB is repeated 4 times
            _cpuRam.Write((ushort)(address % 0x800), value);
            return;
        }

        if (address < 0x4000)
        {
            // PPU register, repeats every 8bytes
            var offset = (ushort)((address - 0x2000) % 8);
            _ppu.Write(offset, value);
            return;
        }

        if (address == 0x4014)
        {
            _oamDmaDevice.Write(0, value);
            return;
        }

        if (address == 0x4016)
        {
            _joystick1.Write(0, value);
            return;
        }

        if (address < 0x4020)
        {
            // APU and disabled I/O. Always disabled
            return;
        }

        // Remainding is the cartridge space
        var cartridgeOffset = (ushort)(address - 0x4020);
        _cartridgeSpace.Write(cartridgeOffset, value);
    }
}
