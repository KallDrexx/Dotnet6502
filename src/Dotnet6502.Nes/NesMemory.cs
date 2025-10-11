using Dotnet6502.Common;
using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Nes;

public class NesMemory : IMemoryMap
{
    private const int UnmappedSpaceStart = 0x4020;

    private readonly Ppu _ppu;
    private readonly INesInput _input;
    private enum MemoryType { InternalRam, Ppu, Apu, Joy1, Joy2, UnmappedSpace, PpuOamDma }
    private readonly byte[] _internalRam = new byte[0x800]; // 2KB
    private readonly byte[] _unmappedSpace = new byte[0x10000 - UnmappedSpaceStart];
    private ControllerState _currentState = new();
    private int _inputBitIndex = 0;

    private readonly ControllerBits[] _bitOrder =
    [
        ControllerBits.A, ControllerBits.B, ControllerBits.Select, ControllerBits.Start, ControllerBits.Up,
        ControllerBits.Down, ControllerBits.Left, ControllerBits.Right
    ];

    public NesMemory(Ppu ppu, byte[] prgRomData, INesInput input)
    {
        _ppu = ppu;
        _input = input;

        if (prgRomData.Length % 0x4000 != 0)
        {
            var message = $"Expected prgRom as multiple of 0x4000, instead it was 0x{prgRomData.Length:X4}";
            throw new InvalidOperationException(message);
        }

        for (var x = 0; x < prgRomData.Length; x++)
        {
            var unmappedSpaceIndex = _unmappedSpace.Length - x - 1;
            var prgRomDataIndex = prgRomData.Length - x - 1;
            _unmappedSpace[unmappedSpaceIndex] = prgRomData[prgRomDataIndex];
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

            case MemoryType.Apu:
                // Ignore APU writes
                break;

            case MemoryType.UnmappedSpace:
                var offsetAddress = address - UnmappedSpaceStart;
                _unmappedSpace[offsetAddress] = value;
                break;

            case MemoryType.PpuOamDma:
                PerformOamDma(value);
                break;

            case MemoryType.Joy1:
                _currentState = _input.GetGamepad1State();
                _inputBitIndex = 0;
                break; // joystick probe / latch.

            case MemoryType.Joy2:
                break; // APU frame counter

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

            case MemoryType.Apu:
                throw new NotImplementedException();

            case MemoryType.UnmappedSpace:
                var offsetAddress = address - UnmappedSpaceStart;
                return _unmappedSpace[offsetAddress];

            case MemoryType.Joy1:
                var currentIndex = _inputBitIndex;
                _inputBitIndex++;
                switch (_bitOrder[currentIndex])
                {
                    case ControllerBits.A: return _currentState.A ? (byte)1 : (byte)0;
                    case ControllerBits.B: return _currentState.B ? (byte)1 : (byte)0;
                    case ControllerBits.Start: return _currentState.Start ? (byte)1 : (byte)0;
                    case ControllerBits.Select: return _currentState.Select ? (byte)1 : (byte)0;
                    case ControllerBits.Up: return _currentState.Up ? (byte)1 : (byte)0;
                    case ControllerBits.Down: return _currentState.Down ? (byte)1 : (byte)0;
                    case ControllerBits.Left: return _currentState.Left ? (byte)1 : (byte)0;
                    case ControllerBits.Right: return _currentState.Right ? (byte)1 : (byte)0;
                }
                return 0;

            case MemoryType.Joy2:
                return 0;

            default:
                throw new NotSupportedException(memoryType.ToString());
        }
    }

    public IReadOnlyList<CodeRegion> GetCodeRegions()
    {
        return
        [
            new CodeRegion(0x0000, _internalRam),
            new CodeRegion(0x0800, _internalRam), // ram mirrors
            new CodeRegion(0x1000, _internalRam),
            new CodeRegion(0x1800, _internalRam),
            new CodeRegion(0x4020, _unmappedSpace),
        ];
    }

    private static MemoryType GetMemoryType(ushort address)
    {
        return address switch
        {
            0x4014 => MemoryType.PpuOamDma,
            0x4016 => MemoryType.Joy1,
            0x4017 => MemoryType.Joy2,
            < 0x2000 => MemoryType.InternalRam,
            < 0x4000 => MemoryType.Ppu,
            < 0x4020 => MemoryType.Apu,
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