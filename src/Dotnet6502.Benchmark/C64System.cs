using System;
using Dotnet6502.C64.Emulation;
using Dotnet6502.C64.Hardware;
using Dotnet6502.C64.Integration;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Common.Macros;
using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.Benchmark;

public class C64System : ISystem
{
    private readonly C64Display _display;
    private readonly C64MemoryConfig _memoryConfig;
    private readonly KeyboardMapping _keyboardMapping = new();
    private readonly Macro? _macro;
    private readonly HashSet<Keys> _pressedMacroKeys = [];
    
    public MemoryBus MemoryBus { get; private set; }
    public CancellationTokenSource CodeCancellationTokenSource { get; } = new();
    public Base6502Hal Hal { get; private set; }
    public Action? OnFrameFinished { get; set; }

    public C64System(CommandLineHandler.C64Config config, Macro? macro)
    {
        _display = new C64Display(this);
        _memoryConfig = SetupMemoryConfig(config);
        var vic2 = new Vic2(_display, _memoryConfig);
        Hal = new C64Hal(_memoryConfig, CodeCancellationTokenSource.Token, vic2, null, false);

        MemoryBus = _memoryConfig.CpuMemoryBus;
        _macro = macro;

        _memoryConfig.IoMemoryArea.Cia1.ExternalPortBInput += () =>
        {
            var columnMask = _memoryConfig.IoMemoryArea.Cia1.DataPortA;
            return _keyboardMapping.GetRowValues(columnMask);
        };
    }

    public ushort GetResetVector()
    {
        var resetVector = (ushort)((_memoryConfig.CpuMemoryBus.Read(0xFFFD) << 8) | _memoryConfig.CpuMemoryBus.Read(0xFFFC));
        return resetVector;
    }

    private C64MemoryConfig SetupMemoryConfig(CommandLineHandler.C64Config config)
    {
        var basicRomContents = File.ReadAllBytes(config.BasicRom.FullName);
        var kernelRomContents = File.ReadAllBytes(config.KernelRom.FullName);
        var charRomContents = File.ReadAllBytes(config.CharRom.FullName);

        var memoryConfig = new C64MemoryConfig();
        memoryConfig.KernelRom.SetContent(kernelRomContents);
        memoryConfig.BasicRom.SetContent(basicRomContents);
        memoryConfig.CharRom.SetContent(charRomContents);

        return memoryConfig;
    }

    public void SetFrameNumber(int frameNumber)
    {
        if (_macro != null)
        {
            MonogameApp.UpdateMacroKeys(_pressedMacroKeys, _macro, frameNumber);
            _keyboardMapping.SetSimulatedKeys(_pressedMacroKeys);
        }
    }

    private class C64Display : IC64Display
    {
        private readonly C64System _system;

        public C64Display(C64System system)
        {
            _system = system;
        }

        public void RenderFrame(RgbColor[] pixels)
        {
            _system.OnFrameFinished?.Invoke();
        }
    }
}
