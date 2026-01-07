using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

public class C64MemoryConfig
{
    public BasicRamMemoryDevice FullRam { get; } = new(0xFFFF + 1);
    public BasicRamMemoryDevice KernelRom { get; } = new(0xFFFF - 0xE000 + 1);
    public BasicRamMemoryDevice CharRom { get; } = new(0xDFFF - 0xD000 + 1);
    public BasicRamMemoryDevice BasicRom { get; } = new(0xBFFF - 0xA000 + 1);
    public IoMemoryArea IoMemoryArea { get; } = new();

    public MemoryBus CpuMemoryBus { get; } = new(0xFFFF + 1);
    public MemoryBus Vic2MemoryBus { get; } = new(0xFFFF + 1);

    public C64MemoryConfig()
    {
        CpuMemoryBus.Attach(FullRam, 0x0000);

        var pla = new ProgrammableLogicArray(this);

        // Set the default map configuration value
        pla.Write(1, 0b00110111);

        // The Vi2 sees the full RAM area, except character rom at 0x1000 and 0x9000.
        Vic2MemoryBus.Attach(FullRam, 0x0000);
        Vic2MemoryBus.Attach(CharRom, 0x1000, true);
        Vic2MemoryBus.Attach(CharRom, 0x9000, true);
    }
}