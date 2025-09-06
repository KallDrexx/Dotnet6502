using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;

var arguments = Environment.GetCommandLineArgs();
if (arguments.Length <= 1)
{
    Console.WriteLine("No NES rom specified");
    return 1;
}

var romFile = arguments[1];
if (!File.Exists(romFile))
{
    Console.WriteLine($"Rom file '{romFile}' does not exist");
    return 2;
}

Console.WriteLine($"Loading rom file '{romFile}'");

var loader = new ROMLoader();
var romInfo = loader.LoadFromFile(romFile);
var programRomData = loader.GetPRGROMData();
var disassembler = new Disassembler(romInfo, programRomData);
disassembler.Disassemble();

var decompiler = new Decompiler(romInfo, disassembler);
decompiler.Decompile();

Console.WriteLine("Hello, World!");
return 0;