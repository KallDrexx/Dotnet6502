using Dotnet6502.Nes.Cli;
using NESDecompiler.Core.ROM;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Nes;

// Parse command line arguments
var commandLineValues = CommandLineHandler.Parse(args);
if (commandLineValues == null)
{
    return 1;
}

var romFile = commandLineValues.RomFile;

Console.WriteLine($"Loading ROM: '{romFile.FullName}'");
var loader = new ROMLoader();
var romInfo = loader.LoadFromFile(romFile.FullName);
var programRomData = loader.GetPRGROMData();
var chrRomData = loader.GetCHRROMData();
Console.WriteLine(romInfo.ToString());

Console.WriteLine("Setting up HAL and JIT compiler...");

var app = new MonogameApp();
var cancellationTokenSource = new CancellationTokenSource();
var ppu = new Ppu(chrRomData, romInfo.MirroringType, app);

var debugWriter = commandLineValues.DebugLogFile != null
    ? new DebugWriter(commandLineValues.DebugLogFile, ppu)
    : null;

var memoryBus = new MemoryBus();
var cpuRam = new BasicRamMemoryDevice(0x800);
var cartridgeSpace = new BasicRamMemoryDevice(0xBFE0);

memoryBus.Attach(cpuRam, 0x0000);
memoryBus.Attach(cpuRam, 0x0800);
memoryBus.Attach(cpuRam, 0x1000);
memoryBus.Attach(cpuRam, 0x1800);

// PPU repeats every 8 bytes until 0x4000
for (var x = 0x2000; x < 0x4000; x += 8)
{
    memoryBus.Attach(ppu, (ushort)x);
}

memoryBus.Attach(new NullMemoryDevice(0x13), 0x4000); // APU not implemented
memoryBus.Attach(new OamDmaDevice(ppu, memoryBus), 0x4014);
memoryBus.Attach(new NullMemoryDevice(1), 0x4015); // sound channel not implemented
memoryBus.Attach(new Joystick1(app), 0x4016);
memoryBus.Attach(new NullMemoryDevice(1), 0x4017); // gamepad 2 not implemented yet
memoryBus.Attach(new NullMemoryDevice(8), 0x4018); // disabled apu/i/o functionality
memoryBus.Attach(cartridgeSpace, 0x4020);

// Map the cartridge data to the end of the cartridge space
if (programRomData.Length % 0x4000 != 0)
{
    var message = $"Expected prgRom as multiple of 0x4000, instead it was 0x{programRomData.Length:X4}";
    throw new InvalidOperationException(message);
}

for (var x = 0; x < programRomData.Length; x++)
{
    var unmappedSpaceIndex = cartridgeSpace.Size - x - 1;
    var prgRomDataIndex = programRomData.Length - x - 1;
    cartridgeSpace.Write((ushort)unmappedSpaceIndex, programRomData[prgRomDataIndex]);
}

var hal = new NesHal(memoryBus, ppu, debugWriter, commandLineValues.IsDebugMode, cancellationTokenSource.Token);

var jitCustomizer = new NesJitCustomizer();
var jitCompiler = new JitCompiler(hal, jitCustomizer, memoryBus);

Console.WriteLine($"Starting at reset vector: {romInfo.ResetVector:X4}");
var nesTask = Task.Run(() =>
{
    jitCompiler.RunMethod(romInfo.ResetVector);
});

app.NesCodeTask = nesTask;
app.Run();

// Cancel the NES thread
cancellationTokenSource.Cancel();

Console.WriteLine("Waiting for NES code to cancel");
while (!nesTask.IsCompleted)
{
    await Task.Delay(1);
}

Console.WriteLine("Done");
return 0;
