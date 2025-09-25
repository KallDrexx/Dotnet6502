using Dotnet6502.Nes.Cli;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;
using System.Reflection;
using System.Runtime.Loader;
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

Console.WriteLine("Disassembling ROM...");
var disassembler = new Disassembler(romInfo, programRomData);
disassembler.Disassemble();

Console.WriteLine("Decompiling ROM...");
var decompiler = new Decompiler(romInfo, disassembler);
decompiler.Decompile();

Console.WriteLine("Generating MSIL...");
var nesGameClass = new NesGameClassBuilder(Path.GetFileNameWithoutExtension(romFile.Name), decompiler, disassembler);
using var gameStreamInMemory = new MemoryStream();
nesGameClass.WriteAssemblyTo(gameStreamInMemory);
gameStreamInMemory.Seek(0, SeekOrigin.Begin);

if (commandLineValues.SaveDll)
{
    var outputDir = commandLineValues.OutputDirectory ?? romFile.DirectoryName!;
    var dllFileName = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(romFile.Name) + ".dll");
    try
    {
        if (File.Exists(dllFileName))
        {
            File.Delete(dllFileName);
        }
    }
    catch (IOException)
    {
        // File doesn't exist or can't be deleted, ignore
    }

    using (var stream = File.OpenWrite(dllFileName))
    {
        gameStreamInMemory.CopyTo(stream);
        gameStreamInMemory.Seek(0, SeekOrigin.Begin);
    }

    Console.WriteLine("Saved assembly to " + dllFileName);
}

if (!commandLineValues.RunEmulation)
{
    Console.WriteLine("Run switch not given. Finished");
    return 0;
}

Console.WriteLine("Loading assembly into application...");
gameStreamInMemory.Seek(0, SeekOrigin.Begin);
var assembly = AssemblyLoadContext.Default.LoadFromStream(gameStreamInMemory);

var game = assembly.GetType(nesGameClass.Type.FullName!);
if (game == null)
{
    var message = $"No type with the name '{nesGameClass.Type.FullName}' found in the assembly";
    throw new InvalidOperationException(message);
}

Console.WriteLine("Creating emulated hardware units");
var ppu = new Ppu(chrRomData);
var memory = new NesMemory(ppu, programRomData);
var hal = new NesHal(memory, ppu);

var halField = game.GetField(nesGameClass.HardwareField.Name);
if (halField == null)
{
    var message = $"The game class did not have a field named '{nesGameClass.HardwareField.Name}' for the HAL";
    throw new InvalidOperationException(message);
}

halField.SetValue(null, hal);

var nmiAddress = romInfo.NmiVector;
if (nmiAddress == 0)
{
    throw new InvalidOperationException("Rom has no known NMI vector");
}

var nmiFunction = decompiler.Functions[nmiAddress];
var nmiMethodInfo = game.GetMethod(nmiFunction.Name);
if (nmiMethodInfo == null)
{
    var message = $"No NMI method exists with the name '{nmiFunction.Name}' on the nes game class";
    throw new InvalidOperationException(message);
}

hal.NmiHandler = nmiMethodInfo;

Console.WriteLine($"Starting at reset vector: {romInfo.ResetVector:X4}");
var resetVectorFunction = decompiler.Functions.GetValueOrDefault(romInfo.ResetVector);
if (resetVectorFunction == null)
{
    var message = $"No decompiled function found for the reset vector address";
    throw new InvalidOperationException(message);
}

var resetMethodVector = game.GetMethod(resetVectorFunction.Name);
resetMethodVector!.Invoke(null, null);


Console.WriteLine("Done");
return 0;