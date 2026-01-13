using Dotnet6502.Common.Hardware;
using Shouldly;

namespace Dotnet6502.Tests.Common.Hardware;

public class MemoryBusTests
{
    [Fact]
    public void GetAllCodeRegions_Returns_Single_Device_Fully_Visible()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x1000);
        bus.Attach(ram, 0x0000);

        var regions = bus.GetAllCodeRegions();

        regions.Count.ShouldBe(1);
        regions[0].BaseAddress.ShouldBe((ushort)0x0000);
        regions[0].Bytes.Length.ShouldBe(0x1000);
    }

    [Fact]
    public void GetAllCodeRegions_Excludes_Completely_Overridden_Device()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x1000);
        var rom = new BasicRamMemoryDevice(0x1000);

        bus.Attach(ram, 0x0000);
        bus.Attach(rom, 0x0000, allowsOverriding: true);

        var regions = bus.GetAllCodeRegions();

        // should only see ROM, not RAM
        regions.Count.ShouldBe(1);
        regions[0].BaseAddress.ShouldBe((ushort)0x0000);

        // Verify it's the ROM by checking that the spans have the same content
        regions[0].Bytes.Span.SequenceEqual(rom.RawBlockFromZero!.Value.Span).ShouldBeTrue();
    }

    [Fact]
    public void GetAllCodeRegions_Handles_Partial_Override_Fragmentation()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x4000); // 0x0000-0x3FFF
        var rom = new BasicRamMemoryDevice(0x1000); // Will override 0x2000-0x2FFF

        bus.Attach(ram, 0x0000);
        bus.Attach(rom, 0x2000, allowsOverriding: true);

        var regions = bus.GetAllCodeRegions();

        // should see RAM fragmented around ROM
        regions.Count.ShouldBe(3);

        // First RAM fragment: 0x0000-0x1FFF
        regions[0].BaseAddress.ShouldBe((ushort)0x0000);
        regions[0].Bytes.Length.ShouldBe(0x2000);

        // ROM: 0x2000-0x2FFF
        regions[1].BaseAddress.ShouldBe((ushort)0x2000);
        regions[1].Bytes.Length.ShouldBe(0x1000);

        // Second RAM fragment: 0x3000-0x3FFF
        regions[2].BaseAddress.ShouldBe((ushort)0x3000);
        regions[2].Bytes.Length.ShouldBe(0x1000);
    }

    [Fact]
    public void GetAllCodeRegions_Handles_Mirrored_Devices()
    {
        // NES-style RAM mirroring
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x0800);

        bus.Attach(ram, 0x0000);
        bus.Attach(ram, 0x0800);
        bus.Attach(ram, 0x1000);
        bus.Attach(ram, 0x1800);

        var regions = bus.GetAllCodeRegions();

        // should see 4 separate regions for each mirror
        regions.Count.ShouldBe(4);

        regions[0].BaseAddress.ShouldBe((ushort)0x0000);
        regions[0].Bytes.Length.ShouldBe(0x0800);

        regions[1].BaseAddress.ShouldBe((ushort)0x0800);
        regions[1].Bytes.Length.ShouldBe(0x0800);

        regions[2].BaseAddress.ShouldBe((ushort)0x1000);
        regions[2].Bytes.Length.ShouldBe(0x0800);

        regions[3].BaseAddress.ShouldBe((ushort)0x1800);
        regions[3].Bytes.Length.ShouldBe(0x0800);

        // All should point to the same underlying memory
        regions[0].Bytes.Span.SequenceEqual(regions[1].Bytes.Span).ShouldBeTrue();
        regions[0].Bytes.Span.SequenceEqual(regions[2].Bytes.Span).ShouldBeTrue();
        regions[0].Bytes.Span.SequenceEqual(regions[3].Bytes.Span).ShouldBeTrue();
    }

    [Fact]
    public void GetAllCodeRegions_Excludes_Devices_Without_RawBlockFromZero()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x1000);
        var nullDevice = new NullMemoryDevice(0x1000);

        bus.Attach(ram, 0x0000);
        bus.Attach(nullDevice, 0x1000);

        var regions = bus.GetAllCodeRegions();

        // should only see RAM, not NullMemoryDevice
        regions.Count.ShouldBe(1);
        regions[0].BaseAddress.ShouldBe((ushort)0x0000);
    }

    [Fact]
    public void GetAllCodeRegions_Returns_Empty_List_For_Empty_Bus()
    {
        var bus = new MemoryBus(0x10000);
        var regions = bus.GetAllCodeRegions();
        regions.Count.ShouldBe(0);
    }

    [Fact]
    public void GetAllCodeRegions_Returns_Current_Memory_Content_After_Write()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x1000);
        bus.Attach(ram, 0x0000);

        // write to memory
        bus.Write(0x0100, 0x42);
        var regions = bus.GetAllCodeRegions();

        // should see the written value
        regions.Count.ShouldBe(1);
        regions[0].Bytes.Span[0x0100].ShouldBe((byte)0x42);
    }

    [Fact]
    public void GetAllCodeRegions_Handles_Complex_C64_Style_Configuration()
    {
        // simulate C64 memory map with overlapping ROMs
        var bus = new MemoryBus(0x10000);
        var fullRam = new BasicRamMemoryDevice(0x10000);
        var charRom = new BasicRamMemoryDevice(0x1000);

        // Full RAM covers everything
        bus.Attach(fullRam, 0x0000);

        // Character ROM overlays at two locations
        bus.Attach(charRom, 0x1000, allowsOverriding: true);
        bus.Attach(charRom, 0x9000, allowsOverriding: true);

        var regions = bus.GetAllCodeRegions();

        // should see RAM fragmented with CharRom overlays
        regions.Count.ShouldBe(5);

        // RAM: 0x0000-0x0FFF
        regions[0].BaseAddress.ShouldBe((ushort)0x0000);
        regions[0].Bytes.Length.ShouldBe(0x1000);

        // CharRom: 0x1000-0x1FFF
        regions[1].BaseAddress.ShouldBe((ushort)0x1000);
        regions[1].Bytes.Length.ShouldBe(0x1000);

        // RAM: 0x2000-0x8FFF
        regions[2].BaseAddress.ShouldBe((ushort)0x2000);
        regions[2].Bytes.Length.ShouldBe(0x7000);

        // CharRom: 0x9000-0x9FFF
        regions[3].BaseAddress.ShouldBe((ushort)0x9000);
        regions[3].Bytes.Length.ShouldBe(0x1000);

        // RAM: 0xA000-0xFFFF
        regions[4].BaseAddress.ShouldBe((ushort)0xA000);
        regions[4].Bytes.Length.ShouldBe(0x6000);
    }

    [Fact]
    public void GetAllCodeRegions_Slices_Device_Memory_Correctly()
    {
        var bus = new MemoryBus(0x10000);
        var ram = new BasicRamMemoryDevice(0x1000);

        // Write test pattern
        for (int i = 0; i < 0x1000; i++)
        {
            ram.Write((ushort)i, (byte)(i & 0xFF));
        }

        var rom = new BasicRamMemoryDevice(0x0100);
        bus.Attach(ram, 0x0000);
        bus.Attach(rom, 0x0800, allowsOverriding: true); // Override middle section

        var regions = bus.GetAllCodeRegions();
        regions.Count.ShouldBe(3);

        // First RAM slice should start with correct offset
        regions[0].Bytes.Span[0].ShouldBe((byte)0x00);
        regions[0].Bytes.Span[0xFF].ShouldBe((byte)0xFF);

        // ROM in middle
        regions[1].BaseAddress.ShouldBe((ushort)0x0800);
        regions[1].Bytes.Length.ShouldBe(0x0100);

        // Second RAM slice should continue from correct offset
        regions[2].BaseAddress.ShouldBe((ushort)0x0900);
        regions[2].Bytes.Span[0].ShouldBe((byte)0x00); // Offset 0x900 in device
    }
}
