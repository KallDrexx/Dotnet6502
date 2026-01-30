namespace Dotnet6502.C64;

public static class CommandLineHandler
{
    public record Values(
        FileInfo? BasicRom,
        FileInfo? KernelRom,
        FileInfo? CharacterRom,
        FileInfo? LogFile,
        FileInfo? DiskImage,
        bool InDebugMode);

    public static Values Parse(string[] args)
    {
        FileInfo? basicRom = null, kernelRom = null, charRom = null, logFile = null, diskImage = null;
        var inDebugMode = false;

        for (var x = 0; x < args.Length; x++)
        {
            switch (args[x].ToLower())
            {
                case "--kernel":
                case "-k":
                    if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                    {
                        kernelRom = new FileInfo(args[++x]);
                    }

                    break;

                case "--basic":
                case "-b":
                    if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                    {
                        basicRom = new FileInfo(args[++x]);
                    }

                    break;

                case "--char":
                case "-c":
                    if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                    {
                        charRom = new FileInfo(args[++x]);
                    }

                    break;

                case "--log":
                case "-l":
                    if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                    {
                        logFile = new FileInfo(args[++x]);
                    }

                    break;

                case "--debug":
                case "-d":
                    inDebugMode = true;
                    break;

                case "--d64":
                    if (x + 1 < args.Length && !args[x + 1].StartsWith("-"))
                    {
                        diskImage = new FileInfo(args[++x]);
                    }

                    break;

            }
        }

        return new Values(basicRom, kernelRom, charRom, logFile, diskImage, inDebugMode);
    }

    public static void ShowHelp()
    {
        Console.WriteLine("""
                          Dotnet6502 Commodore64 Emulator
                          
                          Usage:
                            Dotnet6502.C64 [options]
                            
                          Required options:
                            --kernel     -k <file>     The Kernel ROM to load
                            --basic      -b <file>     The BASIC ROM to load
                            --char       -c <file>     The character rom to load
                            
                          Optional options
                            --log        -l <file>     The file to write instruction log contents to
                            --debug      -d            Enables debug mode which caches instruction
                            --d64                      Location of the d64 disk image to load
                          """);
    }
}