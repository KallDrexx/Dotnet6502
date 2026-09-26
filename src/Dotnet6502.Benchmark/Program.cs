using System.Diagnostics;
using Dotnet6502.Benchmark;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Nes;

namespace Dotnet6502.Benchmark;

public static class Program
{
    private record RunInterval(TimeSpan Timing, int FrameCount, int CompileCount);

    public static async Task<int> Main(string[] args)
    {
        var options = CommandLineHandler.Parse(args);
        if (options == null)
        {
            return 1;
        }

        var framesPerInterval = options.FramesPerInterval ?? 60;
        const int runCount = 5;
        var runs = new Queue<RunInterval>[runCount];

        Console.WriteLine($"Starting {runCount} benchmark runs");
        for (var x = 0; x < runCount; x++)
        {
            var runInfo = RunBenchmark(options);
            if (runInfo == null)
            {
                return 1;
            }

            runs[x] = runInfo;
        }

        Console.WriteLine($"Benchmark finished");
        Console.WriteLine($"Frames Executed: {options.FrameCount}");
        Console.WriteLine($"Frames Per Interval: {framesPerInterval}");
        Console.WriteLine();

        var intervalCount = runs[0].Count;
        if (runs.Any(x => x.Count != intervalCount))
        {
            Console.Error.WriteLine($"All runs do not have the same number of intervals");
            return 1;
        }

        var fileName = $"dn6502-{DateTime.Now:yyyyMMddHHmmss}.csv";
        var path = Path.Combine(Path.GetTempPath(), fileName);
        using (var file = File.Create(path))
        using (var writer = new StreamWriter(file))
        {
            // `#` prefixes are used so some csv tools don't align width on these values
            await writer.WriteLineAsync("#Dotnet6502 Benchmark Run");
            await writer.WriteLineAsync($"#{args.Aggregate((x, y) => $"{x} {y}")}");
            await writer.WriteLineAsync();

            // Write headers
            await writer.WriteAsync("Interval, Frames, ");
            for (var x = 0; x < runs.Length; x++)
            {
                await writer.WriteAsync($"Run {x + 1} ms, ");
            }

            await writer.WriteLineAsync("Average ms, Average ms Per Frame, Compilation Count");

            // Contents
            for (var x = 0; x < intervalCount; x++)
            {
                await writer.WriteAsync($"{x}, ");
                var compilationCount = 0;
                var frameCount = 0;
                var totalMs = 0.0;

                for (var y = 0; y < runs.Length; y++)
                {
                    var info = runs[y].Dequeue();
                    if (y == 0)
                    {
                        // The first run needs to write the frame count
                        await writer.WriteAsync($"{info.FrameCount}, ");
                        frameCount = info.FrameCount;
                        compilationCount = info.CompileCount;
                    }

                    await writer.WriteAsync($"{info.Timing.TotalMilliseconds:0.000}, ");
                    totalMs += info.Timing.TotalMilliseconds;
                }

                var averagePerRun = totalMs / runs.Length;
                var averagePerFrame = averagePerRun / frameCount;
                await writer.WriteAsync($"{averagePerRun:0.000}, {averagePerFrame:0.000}, ");
                await writer.WriteAsync($"{compilationCount}");
                await writer.WriteLineAsync();
            }
        }

        Console.WriteLine($"Benchmark results written to {path}");

        return 0;
    }

    private static Queue<RunInterval>? RunBenchmark(CommandLineHandler.Options options)
    {
        ISystem system;
        if (options.NesConfig != null)
        {
            system = new NesSystem(options.NesConfig);
        }
        else
        {
            Console.Error.WriteLine("No known system specified to benchmark");
            return null;
        }

        var jitCustomizer = new NesJitCustomizer();
        var interpreter = new Ir6502Interpreter();
        jitCustomizer.AddInstructions(interpreter);

        var jitCompiler = new JitCompiler(system.Hal, jitCustomizer, system.MemoryBus, interpreter);

        var framesPerInterval = options.FramesPerInterval ?? 60;
        long frameCount = 0;
        var stopwatch = new Stopwatch();
        var timings = new Queue<RunInterval>((int)Math.Ceiling((decimal)options.FrameCount / framesPerInterval));
        var prevCompileCount = 0;
        var intervalCompileCount = 0;

        system.OnFrameFinished = () =>
        {
            var newCompilationCount = jitCompiler.MethodNotCompiledCount - prevCompileCount;
            prevCompileCount = jitCompiler.MethodNotCompiledCount;
            intervalCompileCount += newCompilationCount;
            
            if (!stopwatch.IsRunning)
            {
                // First frame is missed for accuracy
                stopwatch.Start();
            }
            else
            {
                frameCount++;

                var isNewInterval = false;
                if (frameCount % framesPerInterval == 0)
                {
                    stopwatch.Stop();
                    timings.Enqueue(new RunInterval(stopwatch.Elapsed, framesPerInterval, intervalCompileCount));
                    stopwatch.Restart();
                    isNewInterval = true;
                    intervalCompileCount = 0;
                }

                if (frameCount >= options.FrameCount)
                {
                    stopwatch.Stop();
                    if (!isNewInterval)
                    {
                        timings.Enqueue(new RunInterval(stopwatch.Elapsed, (int)frameCount % framesPerInterval, intervalCompileCount));
                    }

                    system.CodeCancellationTokenSource.Cancel();
                }
            }
        };

        var resetVector = system.GetResetVector();

        try
        {
            jitCompiler.RunMethod(resetVector);
        }
        catch (TaskCanceledException)
        {
            // Expected
        }

        return timings;
    }
}
