using System;
using System.Diagnostics;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Common.Macros;
using Dotnet6502.Nes;
using NESDecompiler.Core.ROM;
using static Dotnet6502.Benchmark.CommandLineHandler;

namespace Dotnet6502.Benchmark;

public class NesSystem : ISystem<NesHal>
{
    private readonly ROMInfo _romInfo;
    private readonly byte[] _programRomData, _chrRomData;
    private readonly NesDisplay _nesDisplay;
    private readonly Ppu _ppu;
    private readonly MacroNesInput? _nesInput;

    public IMemoryBus MemoryBus { get; }
    public CancellationTokenSource CodeCancellationTokenSource { get; }
    public NesHal Hal { get; }
    public Action? OnFrameFinished { get; set; }

    public NesSystem(NesConfig config, Macro? macro)
    {
        Console.WriteLine($"Loading NES ROM: '{config.RomFile.FullName}'");
        var loader = new ROMLoader();

        _romInfo = loader.LoadFromFile(config.RomFile.FullName);
        _programRomData = loader.GetPRGROMData();
        _chrRomData = loader.GetCHRROMData();
        _nesInput = macro != null
            ? new MacroNesInput(macro)
            : null;

        Console.WriteLine(_romInfo.ToString());

        _nesDisplay = new NesDisplay(this);
        _ppu = new Ppu(_chrRomData, _romInfo.MirroringType, _nesDisplay)
        {
            MinimizePpuLogic = config.BypassPpuLogic,
        };

        MemoryBus = new NesMemoryBus(_ppu, (INesInput?)_nesInput ?? new NullInput(), _programRomData);

        CodeCancellationTokenSource = new CancellationTokenSource();
        Hal = new NesHal((NesMemoryBus)MemoryBus, _ppu, null, false, CodeCancellationTokenSource.Token);
    }

    public ushort GetResetVector()
    {
        return _romInfo.ResetVector;
    }

    public void SetFrameNumber(int frameNumber)
    {
        _nesInput?.UpdateForFrameNumber(frameNumber);
    }

    private class NesDisplay(NesSystem parent) : INesDisplay
    {
        private readonly NesSystem _parent = parent;

        public void RenderFrame(RgbColor[] pixels)
        {
            _parent.OnFrameFinished?.Invoke();
        }
    }

    private class NullInput : INesInput
    {
        private readonly ControllerState _controller = new();
        
        public ControllerState GetGamepad1State()
        {
            return _controller;
        }
    }
}
