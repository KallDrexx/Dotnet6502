using System.CommandLine;

namespace DotNetJit.Cli;

public static class CommandLineHandler
{
    public record Values(FileInfo RomFile);

    public static Values? Parse(string[] args)
    {
        var romFileOption = new Option<FileInfo>("--rom", "-r")
        {
            Description = "The NES rom file to read",
            Required = true,
        };

        var rootCommand = new RootCommand("Dotnet JIT runner for NES roms");
        rootCommand.Options.Add(romFileOption);

        var parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Any())
        {
            foreach (var error in parseResult.Errors)
            {
                Console.Error.WriteLine(error.Message);
            }

            return null;
        }

        var romFile = parseResult.GetValue(romFileOption);
        if (romFile is not { Exists: true })
        {
            Console.Error.WriteLine($"Rom file '{romFile!.FullName}' does not exist");
            return null;
        }

        return new Values(romFile);
    }
}