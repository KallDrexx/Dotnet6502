using System;

namespace Dotnet6502.Benchmark;

public static class CommandLineHandler
{
    public record NesConfig(FileInfo RomFile, bool BypassPpuLogic);

    public class C64Config
    {
        public required FileInfo KernelRom { get; init; }
        public required FileInfo BasicRom { get; init; }
        public required FileInfo CharRom { get; init; }
    }

    public class Options
    {
        public NesConfig? NesConfig { get; init; }
        public C64Config? C64Config { get; init; }
        public int FrameCount { get; init; }
        public int? FramesPerInterval { get; init; }
        public bool UseInterpreter { get; init; }
        public FileInfo? Macro { get; init; }
    }

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

            case "c64":
                options = ParseC64Options(ref argsSpan);
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
        var useInterpreter = false;
        var noPpu = false;
        FileInfo? macroFile = null;

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

            if (ParseUseInterpreter(ref args) == true)
            {
                useInterpreter = true;
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

                case "--noppu":
                    args = args[1..];
                    noPpu = true;
                    break;

                case "--macro":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --macro requires a file path");
                        break;
                    }

                    macroFile = new FileInfo(args[0]);
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

        var nesOptions = new NesConfig(romFile, noPpu);
        return new Options
        {
            NesConfig = nesOptions,
            FrameCount = frameCount.Value,
            FramesPerInterval = framesPerInterval,
            UseInterpreter = useInterpreter,
            Macro = macroFile,
        };
    }

    public static Options? ParseC64Options(ref Span<string> args)
    {
        FileInfo? kernel = null, basic = null, charRom = null;
        int? frameCount = null, framesPerInterval = null;
        var useInterpreter = false;
        FileInfo? macroFile = null;

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

            if (ParseUseInterpreter(ref args) == true)
            {
                useInterpreter = true;
                continue;
            }

            switch (args[0])
            {
                case "--kernel":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --kernel requires a file path");
                        break;
                    }

                    kernel = new FileInfo(args[0]);
                    args = args[1..];

                    break;

                case "--basic":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --basic requires a file path");
                        break;
                    }

                    basic = new FileInfo(args[0]);
                    args = args[1..];

                    break;
                case "--char":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --char requires a file path");
                        break;
                    }

                    charRom = new FileInfo(args[0]);
                    args = args[1..];

                    break;
                
                case "--macro":
                    args = args[1..];
                    if (args.IsEmpty)
                    {
                        Console.Error.WriteLine($"Error: --macro requires a file path");
                        break;
                    }

                    macroFile = new FileInfo(args[0]);
                    args = args[1..];
                    break;


                default:
                    Console.Error.WriteLine($"Unknown option: {args[0]}");
                    break;

            }
        }

        if (kernel == null || basic == null || charRom == null)
        {
            Console.Error.WriteLine("Error: a kernel, basic, and character rom is required");
            return null;
        }

        if (frameCount == null)
        {
            Console.Error.WriteLine("Error: No frame count specified (--frames)");
            return null;
        }

        var c64Config = new C64Config()
        {
            KernelRom = kernel,
            BasicRom = basic,
            CharRom = charRom,
        };

        return new Options()
        {
            C64Config = c64Config,
            FrameCount = frameCount.Value,
            FramesPerInterval = framesPerInterval,
            UseInterpreter = useInterpreter,
            Macro = macroFile,
        };
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

    private static bool? ParseUseInterpreter(ref Span<string> args)
    {
        if (args.IsEmpty || (args[0] != "--interpreter"))
        {
            return null;
        }

        args = args[1..];
        return true;
    }

    private static void ShowHelp()
    {
        const string helpText = @"""Dotnet 6502 Benchmarking application

Runs a specific number of frames for a 6502 system, and provides performance results.

Usage:
  Dotnet6502.Benchmark <System> --frames <count> <SystemOptions>

Required:
  --frames <number>   The number of frames to execute before quitting

Optional:
  --help               Show this help message
  --interval <number>  The number of frames per interval
  --interpreter        Use the interpreter instead of JIT compilation

Systems:
  nes            Runs the NES system for benchmarking
  c64            Runs the C64 system for benchmarking

Nes Options:
  --rom <file-path>    (Required) The NES ROM file to run
  --noppu              If specified, bypasses most PPU logic

C64 Options:
  --kernel <file-path> (Required) The C64 Kernel rom to load
  --basic  <file-path> (Required) The C64 Basic rom to load
  --char   <file-path> (Required) The C64 Character rom to load

Examples:
  Dotnet6502.Benchmark nes --rom smb.nes --frames 2200 --interval 60 --macro macros/smb-benchmark.macro
  Dotnet6502.Benchmark c64 --char c64-chars.bin --basic c64-basic.bin --kernel c64-kernel.rom --frames 1000 --macro macros/c64-basic-borders.macro
""";

        Console.WriteLine(helpText);
    }
}
