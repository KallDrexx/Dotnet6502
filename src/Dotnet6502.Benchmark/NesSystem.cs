using System;
using System.Diagnostics;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Nes;
using NESDecompiler.Core.ROM;
using static Dotnet6502.Benchmark.CommandLineHandler;

namespace Dotnet6502.Benchmark;

public class NesSystem : BenchmarkingSystem
{
    private readonly ROMInfo _romInfo;
    private readonly byte[] _programRomData, _chrRomData;
    private readonly NesDisplay _nesDisplay;
    private readonly Ppu _ppu;

    public override Queue<TimeSpan> LoggedTimings => _nesDisplay.LoggedTimes;

    public NesSystem(NesConfig config, int framesPerTimeLog) : base(framesPerTimeLog)
    {
        Console.WriteLine($"Loading NES ROM: '{config.RomFile.FullName}'");
        var loader = new ROMLoader();

        _romInfo = loader.LoadFromFile(config.RomFile.FullName);
        _programRomData = loader.GetPRGROMData();
        _chrRomData = loader.GetCHRROMData();

        Console.WriteLine(_romInfo.ToString());

        _nesDisplay = new NesDisplay(framesPerTimeLog);
        _ppu = new Ppu(_chrRomData, _romInfo.MirroringType, _nesDisplay);

    }

    private void SetupMemoryBus()
    {
        // TODO: Share this with Nes.Cli at some point
        MemoryBus = new MemoryBus(0xFFFF + 1);
        var cpuRam = new BasicRamMemoryDevice(0x800);
        var cartridgeSpace = new BasicRamMemoryDevice(0xBFE0);
        
        MemoryBus.Attach(cpuRam, 0x0000);
        MemoryBus.Attach(cpuRam, 0x0800);
        MemoryBus.Attach(cpuRam, 0x1000);
        MemoryBus.Attach(cpuRam, 0x1800);
        
        // PPU repeats every 8 bytes until 0x4000
        for (var x = 0x2000; x < 0x4000; x += 8)
        {
            MemoryBus.Attach(_ppu, (ushort)x);
        }
        
        MemoryBus.Attach(new NullMemoryDevice(0x13), 0x4000); // APU not implemented
        MemoryBus.Attach(new OamDmaDevice(_ppu, MemoryBus), 0x4014);
        MemoryBus.Attach(new NullMemoryDevice(1), 0x4015); // sound channel not implemented
        MemoryBus.Attach(new NullMemoryDevice(1), 0x4016);
        MemoryBus.Attach(new NullMemoryDevice(1), 0x4017); // gamepad 2 not implemented yet
        MemoryBus.Attach(new NullMemoryDevice(8), 0x4018); // disabled apu/i/o functionality
        MemoryBus.Attach(cartridgeSpace, 0x4020);
        
        // Map the cartridge data to the end of the cartridge space
        if (_programRomData.Length % 0x4000 != 0)
        {
            var message = $"Expected prgRom as multiple of 0x4000, instead it was 0x{_programRomData.Length:X4}";
            throw new InvalidOperationException(message);
        }
        
        for (var x = 0; x < _programRomData.Length; x++)
        {
            var unmappedSpaceIndex = cartridgeSpace.Size - x - 1;
            var prgRomDataIndex = _programRomData.Length - x - 1;
            cartridgeSpace.Write((ushort)unmappedSpaceIndex, _programRomData[prgRomDataIndex]);
        }
    }

    private class NesDisplay : INesDisplay
    {
        private readonly Stopwatch _timer = new();

        private readonly int _framesPerTimeLog; // How many frames before logging time
        private int _frameCount;

        public Queue<TimeSpan> LoggedTimes { get; } = new(1000);

        public NesDisplay(int framesPerTimeLog)
        {
            _framesPerTimeLog = framesPerTimeLog;
        }

        public void RenderFrame(RgbColor[] pixels)
        {
            // The timer is off at first, so if it's off start it. This will lose the
            // first frame but that's ok.
            if (!_timer.IsRunning)
            {
                _timer.Start();
            }
            else
            {
                _frameCount++;
            }

            if (_frameCount >= _framesPerTimeLog)
            {
                _timer.Stop();
                LoggedTimes.Enqueue(_timer.Elapsed);
                _timer.Restart();
            }

        }
    }
}
