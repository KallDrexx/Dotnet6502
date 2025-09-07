using DotNetJit.Cli;
using DotNetJit.Cli.Builder;
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

var builder = new NesAssemblyBuilder(Path.GetFileNameWithoutExtension(romFile.Name), decompiler);

var dllFileName = Path.Combine(romFile.DirectoryName!, Path.GetFileNameWithoutExtension(romFile.Name) + ".dll");
try
{
    File.Delete(dllFileName);
}
catch (IOException)
{
    // File doesn't exist, ignore
}

using var dllFile = File.Create(dllFileName);
builder.Save(dllFile);

Console.WriteLine($"Wrote dll: {dllFileName}");

return 0;