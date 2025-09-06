using DotNetJit.Cli;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;

var commandLineValues = CommandLineHandler.Parse(args);
if (commandLineValues == null)
{
    // Errors already written to stderr
    return 1;
}

var romFile = commandLineValues.RomFile;
Console.WriteLine($"Loading rom file '{romFile.FullName}'");

var loader = new ROMLoader();
var romInfo = loader.LoadFromFile(romFile.FullName);
var programRomData = loader.GetPRGROMData();
var disassembler = new Disassembler(romInfo, programRomData);
disassembler.Disassemble();

var decompiler = new Decompiler(romInfo, disassembler);
decompiler.Decompile();

return 0;