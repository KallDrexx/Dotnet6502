namespace Dotnet6502.Nes.Cli;

public static class CommandLineHandler
{
    public record Values(FileInfo RomFile, FileInfo? DebugLogFile, DebugLogSections? Sections);

    public static Values? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return null;
        }

        FileInfo? romFile = null;
        FileInfo? debugLogFile = null;
        DebugLogSections? debugLogSections = null;

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

                case "--debug":
                case "-d":
                    if (i + 1 < args.Length)
                    {
                        debugLogFile = new FileInfo(args[++i]);
                    }
                    else
                    {
                        Console.Error.WriteLine("Error: --debug requires a file path");
                        return null;
                    }
                    break;

                case "--debugSection":
                case "-s":
                    if (i + 1 < args.Length)
                    {
                        var sectionName = args[++i];
                        switch (sectionName.ToLower())
                        {
                            case "all":
                                debugLogSections = DebugLogSections.All;
                                break;

                            case "onlynmi":
                                debugLogSections = DebugLogSections.OnlyNmi;
                                break;

                            case "notnmi":
                                debugLogSections = DebugLogSections.OnlyNonNmi;
                                break;

                            default:
                                Console.Error.WriteLine($"Error: Invalid debug section of '{sectionName}'. " +
                                                        $"Valid values are: all, OnlyNmi, NotNmi");
                                return null;
                        }
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

        return new Values(romFile, debugLogFile, debugLogSections);
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