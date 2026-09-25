using System.Diagnostics;
using Dotnet6502.Benchmark;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Nes;

namespace Dotnet6502.Benchmark;

public static class Program
{
    private record RunInterval(TimeSpan Timing, int FrameCount);

    public static int Main(string[] args)
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

        for (var x = 0; x < intervalCount; x++)
        {
            
        }


        var intervalNum = 0;
        while (timings.TryDequeue(out var data))
        {
            intervalNum++;
            var averageTime = data.Timing.TotalMilliseconds / data.FrameCount;

            Console.Write($"{intervalNum:000}: ");
            Console.Write($"average: {averageTime:00.00}ms ");
            Console.Write($"total: {data.Timing.TotalMilliseconds:000.00}ms ");

            if (data.FrameCount != framesPerInterval)
            {
                Console.Write($"({data.FrameCount} frames)");
            }

            Console.WriteLine();
        }


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

        system.OnFrameFinished = () =>
        {
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
                    timings.Enqueue(new RunInterval(stopwatch.Elapsed, framesPerInterval));
                    stopwatch.Restart();
                    isNewInterval = true;
                }

                if (frameCount >= options.FrameCount)
                {
                    stopwatch.Stop();
                    if (!isNewInterval)
                    {
                        timings.Enqueue(new RunInterval(stopwatch.Elapsed, (int)frameCount % framesPerInterval));
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
