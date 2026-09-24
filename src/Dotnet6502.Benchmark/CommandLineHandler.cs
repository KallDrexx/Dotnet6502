using System;

namespace Dotnet6502.Benchmark;

public static class CommandLineHandler
{
    public record NesConfig(FileInfo RomFile);

    public record Options(NesConfig? NesConfig, int FrameCount, int? FramesPerInterval);

    public static Options? Parse(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return null;
        }

        var systemName = args[0];
        var argsSpan = args.AsSpan()[1..];

        // First argument should be the system
        Options? options = null;
        switch (systemName)
        {
            case "nes":
                options = ParseNesOptions(ref argsSpan);
                break;

            default:
                Console.Error.WriteLine($"System of type {args[0]} is not recognized");
                break;
        }

        if (options == null)
        {
            ShowHelp();
        }

        return options;
    }

    private static Options? ParseNesOptions(ref Span<string> args)
    {
        FileInfo? romFile = null;
        int? frameCount = null;
        int? framesPerInterval = null;

        while (!args.IsEmpty)
        {
            var newFrameCount = ParseFrameCount(ref args);
            if (newFrameCount != null)
            {
                frameCount = newFrameCount;
                continue;
            }

            var newFramesPerInterval = ParseFramesPerInterval(ref args);
            if (newFramesPerInterval != null)
            {
                framesPerInterval = newFramesPerInterval;
                continue;
            }

            switch (args[0])
            {
                case "--rom":
                case "-r":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --rom requires a file path");
                        break;
                    }

                    romFile = new FileInfo(args[0]);
                    args = args[1..];
                    break;

                default:
                    Console.Error.WriteLine($"Error: Unknown option '{args[0]}'");
                    return null;
            }
        }

        if (romFile == null)
        {
            Console.Error.WriteLine("No NES rom file specified (--rom)");
            return null;
        }

        if (frameCount == null)
        {
            Console.Error.WriteLine("Error: No frame count specified (--frames)");
            return null;
        }

        var nesOptions = new NesConfig(romFile);
        var options = new Options(nesOptions, frameCount.Value, framesPerInterval);
        return options;
    }

    private static int? ParseFramesPerInterval(ref Span<string> args)
    {
        if (args.IsEmpty || (args[0] != "--interval" && args[0] != "-i"))
        {
            return null;
        }

        args = args[1..];
        if (args.IsEmpty || !args[0].All(Char.IsDigit))
        {
            Console.Error.WriteLine("No frame count specified");
            return null;
        }

        var count = int.Parse(args[0]);
        args = args[1..];

        return count;
    }

    private static int? ParseFrameCount(ref Span<string> args)
    {
        if (args.IsEmpty || (args[0] != "--frames" && args[0] != "-f"))
        {
            return null;
        }

        args = args[1..];
        if (args.IsEmpty || !args[0].All(Char.IsDigit))
        {
            Console.Error.WriteLine("No frame count specified");
            return null;
        }

        var count = int.Parse(args[0]);
        args = args[1..];

        return count;
    }

    private static void ShowHelp()
    {
        const string helpText = @"""Dotnet 6502 Benchmarking application

Runs a specific number of frames for a 6502 system, and provides performance results.

Usage:
  Dotnet6502.Benchmark <System> --frames <count> <SystemOptions>

Required:
  --frames, -f   The number of frames to execute before quitting

Optional:
  --help,     -h   Show this help message
  --interval, -i   The number of frames per interval

Systems:
  nes            Runs the NES system for benchmarking

Nes Options:
  --rom,    -r   (Required) The NES ROM file to run

Examples:
  Dotnet6502.Benchmark --system nes smb.nes --frames 360
""";

        Console.WriteLine(helpText);
    }
}
