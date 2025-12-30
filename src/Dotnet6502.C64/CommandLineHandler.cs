namespace Dotnet6502.C64;

public static class CommandLineHandler
{
    public record Values(FileInfo? BasicRom, FileInfo? KernelRom, FileInfo? CharacterRom);

    public static Values Parse(string[] args)
    {
        FileInfo? basicRom = null, kernelRom = null, charRom = null;

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
            }
        }

        return new Values(basicRom, kernelRom, charRom);
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
                          """);
    }
}