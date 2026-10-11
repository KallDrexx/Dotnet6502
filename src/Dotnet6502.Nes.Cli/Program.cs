using Dotnet6502.Nes.Cli;
using NESDecompiler.Core.ROM;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Nes;
using Dotnet6502.Common.Macros;

// Parse command line arguments
var commandLineValues = CommandLineHandler.Parse(args);
if (commandLineValues == null)
{
    return 1;
}

Macro? macro = null;
if (commandLineValues.MacroFile != null)
{
    using var file = commandLineValues.MacroFile.OpenRead();
    macro = await Macro.ParseAsync(file);
}

var (romInfo, programRomData, chrRomData) = ParseRom(commandLineValues);
var (app, nesCodeCancellationTokenSource, memoryBus, hal) = SetupHardware(
    chrRomData,
    romInfo,
    commandLineValues,
    programRomData,
    macro);

var jitCustomizer = new NesJitCustomizer()
{
    WriteDebugStrings = commandLineValues.IsDebugMode || commandLineValues.DebugLogFile != null,
};

var interpreter = new Ir6502Interpreter();
jitCustomizer.AddInstructions(interpreter);

var jitCompiler = new JitCompiler<NesHal>(hal, jitCustomizer, memoryBus, interpreter)
{
    AddDebugHooks = commandLineValues.IsDebugMode || commandLineValues.DebugLogFile != null,
};

await RunRom(romInfo, jitCompiler, app, nesCodeCancellationTokenSource);

Console.WriteLine("Done");
return 0;

static (ROMInfo, byte[] ProgramRomData, byte[] ChrRomData) ParseRom(CommandLineHandler.Values values)
{
    var romFile = values.RomFile;

    Console.WriteLine($"Loading ROM: '{romFile.FullName}'");
    var loader = new ROMLoader();
    var romInfo1 = loader.LoadFromFile(romFile.FullName);
    var programRomData = loader.GetPRGROMData();
    var chrRomData = loader.GetCHRROMData();

    Console.WriteLine(romInfo1.ToString());

    return (romInfo1, programRomData, chrRomData);
}

static (MonogameApp, CancellationTokenSource, NesMemoryBus, NesHal) SetupHardware(
    byte[] chrRomData,
    ROMInfo romInfo2,
    CommandLineHandler.Values commandLineValues1,
    byte[] programRomData,
    Macro? macro1)
{
    Console.WriteLine("Setting up HAL and JIT compiler...");

    var monogameApp = new MonogameApp(false, macro1);
    var cancellationTokenSource = new CancellationTokenSource();
    var ppu = new Ppu(chrRomData, romInfo2.MirroringType, monogameApp);

    var debugWriter = commandLineValues1.DebugLogFile != null
        ? new DebugWriter(commandLineValues1.DebugLogFile, ppu)
        : null;

    var memoryBus = new NesMemoryBus(ppu, monogameApp, programRomData);

    var nesHal = new NesHal(memoryBus, ppu, debugWriter, commandLineValues1.IsDebugMode, cancellationTokenSource.Token);
    return (monogameApp, cancellationTokenSource, memoryBus, nesHal);
}

static async Task RunRom(
    ROMInfo romInfo, 
    JitCompiler<NesHal> jitCompiler, 
    MonogameApp app,
    CancellationTokenSource cancellationTokenSource)
{
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
}
