using Dotnet6502.C64;
using Dotnet6502.C64.Hardware;
using Dotnet6502.C64.Integration;
using Dotnet6502.C64.Media;
using Dotnet6502.C64.Patches;
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
var keyboardMapping = new KeyboardMapping();
var macroExecutor = SetupMacroExecutor();
var app = new MonogameApp(keyboardMapping, false, macroExecutor);
var vic2 = new Vic2(app, memoryConfig);
var hal = new C64Hal(memoryConfig, cancellationTokenSource.Token, vic2, logWriter, cliArgs.InDebugMode);
var interpreter = new Ir6502Interpreter();
var jitCustomizer = new C64JitCustomizer();
jitCustomizer.AddInstructions(interpreter);

var jitCompiler = new JitCompiler(hal, jitCustomizer, memoryConfig.CpuMemoryBus, interpreter);
// jitCompiler.AlwaysUseInterpreter = true;

// Add patches
if (cliArgs.DiskImage != null)
{
    var image = D64Image.Load(cliArgs.DiskImage.FullName);
    jitCompiler.AddPatch(new DiskImageReadPatch(image));
}
else if (cliArgs.PrgFile != null)
{
    await AddPrgPatch();
}

// Hook into CIA1's port B to check for keyboard scanning requests
memoryConfig.IoMemoryArea.Cia1.ExternalPortBInput += () =>
{
    var columnMask = memoryConfig.IoMemoryArea.Cia1.DataPortA;
    return keyboardMapping.GetRowValues(columnMask);
};

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

async Task AddPrgPatch()
{
    if (cliArgs.PrgFile == null)
    {
        return;
    }

    Console.WriteLine($"Loading prg file {cliArgs.PrgFile.FullName}");

    byte[] data;
    await using (var stream = cliArgs.PrgFile.OpenRead())
    {
        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        data = memoryStream.ToArray();
    }

    if (data.Length < 2)
    {
        Console.WriteLine("Empty PRG file provided");
        return;
    }

    jitCompiler.AddPatch(new DiskImageReadPatch(data));
}

MacroExecutor? SetupMacroExecutor()
{
    if (cliArgs.MacroFile == null)
    {
        return null;
    }

    if (!cliArgs.MacroFile.Exists)
    {
        Console.WriteLine($"Error: Macro file not found: {cliArgs.MacroFile.FullName}");
        Environment.Exit(1);
    }

    Console.WriteLine($"Loading macro file {cliArgs.MacroFile.FullName}");

    try
    {
        var instructions = MacroParser.ParseFile(cliArgs.MacroFile.FullName);
        Console.WriteLine($"Loaded {instructions.Count} macro instructions");
        return new MacroExecutor(instructions);
    }
    catch (FormatException ex)
    {
        Console.WriteLine($"Error parsing macro file: {ex.Message}");
        Environment.Exit(1);
        return null;
    }
}