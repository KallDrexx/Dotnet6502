using Dotnet6502.C64;
using Dotnet6502.C64.Hardware;
using Dotnet6502.C64.Integration;
using Dotnet6502.Common.Compilation;

var cancellationTokenSource = new CancellationTokenSource();
var cliArgs = CommandLineHandler.Parse(args);

Console.WriteLine("Starting Dotnet6502 Commodore64 emulator");

DebugWriter? logWriter = null;
if (cliArgs.LogFile != null)
{
    logWriter = new DebugWriter(cliArgs.LogFile);
}

var memoryConfig = await SetupMemory();
var app = new MonogameApp(false);
var vic2 = new Vic2(app, memoryConfig);
var hal = new C64Hal(memoryConfig, cancellationTokenSource.Token, vic2, logWriter, cliArgs.InDebugMode);
var jitCustomizer = new C64JitCustomizer();
var jitCompiler = new JitCompiler(hal, jitCustomizer, memoryConfig.CpuMemoryBus);
await RunSystem();

Console.WriteLine("Done");
return 0;

async Task<C64MemoryConfig> SetupMemory()
{
    if (cliArgs.KernelRom == null)
    {
        Console.WriteLine("Error: No kernel rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    if (cliArgs.BasicRom == null)
    {
        Console.WriteLine("Error: No basic rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    if (cliArgs.CharacterRom == null)
    {
        Console.WriteLine("Error: No character rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    var basicRomContents = await File.ReadAllBytesAsync(cliArgs.BasicRom.FullName);
    var kernelRomContents = await File.ReadAllBytesAsync(cliArgs.KernelRom.FullName);
    var charRomContents = await File.ReadAllBytesAsync(cliArgs.CharacterRom.FullName);

    var config = new C64MemoryConfig();
    config.KernelRom.SetContent(kernelRomContents);
    config.BasicRom.SetContent(basicRomContents);
    config.CharRom.SetContent(charRomContents);

    return config;
}

async Task RunSystem()
{
    var resetVector = (ushort)((memoryConfig.CpuMemoryBus.Read(0xFFFD) << 8) | memoryConfig.CpuMemoryBus.Read(0xFFFC));
    Console.WriteLine($"Starting at reset vector {resetVector:X4}");

    var c64Task = Task.Run(() =>
    {
        jitCompiler.RunMethod(resetVector);
    });

    app.C64CodeTask = c64Task;
    app.Run();

    // Cancel the C64 6502 thread
    cancellationTokenSource.Cancel();
    Console.WriteLine("Waiting for C64 code to cancel");

    while (!c64Task.IsCompleted)
    {
        await Task.Delay(1);
    }
}