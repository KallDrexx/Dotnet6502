namespace Dotnet6502.Nes.Cli;

public static class CommandLineHandler
{
    public record Values(FileInfo RomFile);

    public static Values? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return null;
        }

        FileInfo? romFile = null;

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

        return new Values(romFile);
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
        Console.WriteLine("  --help, -h              Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Dotnet6502.Nes.Cli --rom game.nes");
    }
}