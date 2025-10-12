using Dotnet6502.Nes.Cli;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;
using Dotnet6502.Common.Compilation;
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
var memory = new NesMemory(ppu, programRomData, app);

var debugWriter = commandLineValues.DebugLogFile != null
    ? new DebugWriter(commandLineValues.DebugLogFile, ppu)
    : null;

var hal = new NesHal(memory, ppu, debugWriter, cancellationTokenSource.Token);

var jitCustomizer = new NesJitCustomizer();
var jitCompiler = new JitCompiler(hal, jitCustomizer, memory);

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
