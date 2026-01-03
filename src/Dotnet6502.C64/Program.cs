using Dotnet6502.C64;
using Dotnet6502.C64.Emulation;
using Dotnet6502.C64.Hardware;
using Dotnet6502.C64.Integration;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;

var cancellationTokenSource = new CancellationTokenSource();
var cliArgs = CommandLineHandler.Parse(args);

Console.WriteLine("Starting Dotnet6502 Commodore64 emulator");
var ioMemoryArea = new IoMemoryArea();
var pla = await SetupPla(cliArgs);
var memoryBus = SetupMemoryBus();
var app = new MonogameApp(false);
var vic2 = new Vic2(app, ioMemoryArea);
var hal = new C64Hal(memoryBus, cancellationTokenSource.Token, vic2);
var jitCustomizer = new C64JitCustomizer();
var jitCompiler = new JitCompiler(hal, jitCustomizer, memoryBus);
await RunSystem();

Console.WriteLine("Done");
return 0;

async Task<ProgrammableLogicArray> SetupPla(CommandLineHandler.Values values)
{
    if (values.KernelRom == null)
    {
        Console.WriteLine("Error: No kernel rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    if (values.BasicRom == null)
    {
        Console.WriteLine("Error: No basic rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    if (values.CharacterRom == null)
    {
        Console.WriteLine("Error: No character rom specified");
        Console.WriteLine();
        CommandLineHandler.ShowHelp();

        Environment.Exit(1);
    }

    var basicRomContents = await File.ReadAllBytesAsync(values.BasicRom.FullName);
    var kernelRomContents = await File.ReadAllBytesAsync(values.KernelRom.FullName);
    var charRomContents = await File.ReadAllBytesAsync(values.CharacterRom.FullName);

    var programmableLogicArray = new ProgrammableLogicArray(ioMemoryArea);
    programmableLogicArray.BasicRom.SetContent(basicRomContents);
    programmableLogicArray.KernelRom.SetContent(kernelRomContents);
    programmableLogicArray.CharacterRom.SetContent(charRomContents);

    // Set the default map configuration value
    programmableLogicArray.Write(1, 0b00110111);

    return programmableLogicArray;
}

MemoryBus SetupMemoryBus()
{
    var memoryBus1 = new MemoryBus();
    memoryBus1.Attach(pla, 0x0000);
    pla.AttachToBus(memoryBus1);

    // fill in the rest with ram
    memoryBus1.Attach(new BasicRamMemoryDevice(0x7fff - 0x0002 + 1), 0x0002);
    memoryBus1.Attach(new BasicRamMemoryDevice(0xcfff - 0xc000 + 1), 0xc000);

    // TODO: Add cartridge rom low swapping to this region
    memoryBus1.Attach(new BasicRamMemoryDevice(0x9fff - 0x8000 + 1), 0x8000);
    return memoryBus1;
}

async Task RunSystem()
{
    var resetVector = (ushort)((memoryBus.Read(0xFFFD) << 8) | memoryBus.Read(0xFFFC));
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