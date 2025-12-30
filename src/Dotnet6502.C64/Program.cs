using Dotnet6502.C64;
using Dotnet6502.C64.Hardware;
using Dotnet6502.Common.Hardware;

var cliArgs = CommandLineHandler.Parse(args);
if (cliArgs.KernelRom == null)
{
    Console.WriteLine("Error: No kernel rom specified");
    Console.WriteLine();
    CommandLineHandler.ShowHelp();

    return 1;
}

if (cliArgs.BasicRom == null)
{
    Console.WriteLine("Error: No basic rom specified");
    Console.WriteLine();
    CommandLineHandler.ShowHelp();

    return 1;
}

if (cliArgs.CharacterRom == null)
{
    Console.WriteLine("Error: No character rom specified");
    Console.WriteLine();
    CommandLineHandler.ShowHelp();

    return 1;
}

var basicRomContents = await File.ReadAllBytesAsync(cliArgs.BasicRom.FullName);
var kernelRomContents = await File.ReadAllBytesAsync(cliArgs.KernelRom.FullName);
var charRomContents = await File.ReadAllBytesAsync(cliArgs.CharacterRom.FullName);

var pla = new ProgrammableLogicArray();
pla.BasicRom.SetContent(basicRomContents);
pla.KernelRom.SetContent(kernelRomContents);
pla.CharacterRom.SetContent(charRomContents);

var memoryBus = new MemoryBus();
memoryBus.Attach(pla, 0x0000);
pla.AttachToBus(memoryBus);

// fill in the rest with ram
memoryBus.Attach(new BasicRamMemoryDevice(0x7fff - 0x0002 + 1), 0x0002);
memoryBus.Attach(new BasicRamMemoryDevice(0xcfff - 0xc000 + 1), 0xc000);

// TODO: Add cartridge rom low swapping to this region
memoryBus.Attach(new BasicRamMemoryDevice(0x9fff - 0x8000 + 1), 0x8000);

return 0;