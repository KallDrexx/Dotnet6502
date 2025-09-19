using System;
using System.IO;
using System.Linq;

namespace Dotnet6502.Nes.Cli;

public static class CommandLineHandler
{
    public record Values(
        FileInfo RomFile,
        bool RunEmulation,
        string EmulationMode,
        bool SaveDll,
        string? OutputDirectory,
        bool Verbose);

    public static Values? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return null;
        }

        FileInfo? romFile = null;
        bool runEmulation = false;
        string emulationMode = "event-driven";
        bool saveDll = true;
        string? outputDirectory = null;
        bool verbose = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--rom":
                case "-r":
                    if (i + 1 < args.Length)
                    {
                        romFile = new FileInfo(args[++i]);
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: --rom requires a file path");
                        return null;
                    }
                    break;

                case "--run":
                case "-e":
                    runEmulation = true;
                    break;

                case "--mode":
                case "-m":
                    if (i + 1 < args.Length)
                    {
                        emulationMode = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: --mode requires a value");
                        return null;
                    }
                    break;

                case "--save-dll":
                case "-s":
                    saveDll = true;
                    break;

                case "--no-save-dll":
                    saveDll = false;
                    break;

                case "--output":
                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputDirectory = args[++i];
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: --output requires a directory path");
                        return null;
                    }
                    break;

                case "--verbose":
                case "-v":
                    verbose = true;
                    break;

                case "--help":
                case "-h":
                    ShowHelp();
                    return null;

                default:
                    Console.Error.WriteLine($"Error: Unknown option '{args[i]}'");
                    ShowHelp();
                    return null;
            }
        }

        // Validate required arguments
        if (romFile == null)
        {
            Console.Error.WriteLine("Error: ROM file is required. Use --rom <file>");
            ShowHelp();
            return null;
        }

        if (!romFile.Exists)
        {
            Console.Error.WriteLine($"Error: ROM file '{romFile.FullName}' does not exist");
            return null;
        }

        if (!IsValidEmulationMode(emulationMode))
        {
            Console.Error.WriteLine($"Error: Invalid emulation mode '{emulationMode}'. " +
                                  "Valid modes: event-driven, cycle-accurate, instruction-based, hybrid");
            return null;
        }

        if (!string.IsNullOrEmpty(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: Could not create output directory '{outputDirectory}': {ex.Message}");
                return null;
            }
        }

        return new Values(
            romFile,
            runEmulation,
            emulationMode.ToLower(),
            saveDll,
            outputDirectory,
            verbose
        );
    }

    private static void ShowHelp()
    {
        Console.WriteLine("DotNet JIT Compiler and Emulator for NES ROMs");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  Dotnet6502.Nes.Cli --rom <file> [options]");
        Console.WriteLine();
        Console.WriteLine("Required:");
        Console.WriteLine("  --rom, -r <file>        The NES ROM file to process");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --run, -e               Run the emulation after compilation");
        Console.WriteLine("  --mode, -m <mode>       Emulation mode (default: event-driven)");
        Console.WriteLine("                          Valid modes: event-driven, cycle-accurate,");
        Console.WriteLine("                                      instruction-based, hybrid");
        Console.WriteLine("  --save-dll, -s          Save the compiled DLL to disk (default: true)");
        Console.WriteLine("  --no-save-dll           Don't save the compiled DLL to disk");
        Console.WriteLine("  --output, -o <dir>      Output directory for generated files");
        Console.WriteLine("  --verbose, -v           Enable verbose output");
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Dotnet6502.Nes.Cli --rom game.nes");
        Console.WriteLine("  Dotnet6502.Nes.Cli --rom game.nes --run --mode cycle-accurate");
        Console.WriteLine("  Dotnet6502.Nes.Cli --rom game.nes --run --verbose --output ./compiled");
    }

    private static bool IsValidEmulationMode(string mode)
    {
        return mode.ToLower() switch
        {
            "event-driven" or "cycle-accurate" or "instruction-based" or "hybrid" => true,
            _ => false
        };
    }
}