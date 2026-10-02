using System.Diagnostics;
using Dotnet6502.C64.Hardware;
using Dotnet6502.C64.Integration;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Hardware;
using Dotnet6502.Common.Macros;
using Dotnet6502.Nes;

namespace Dotnet6502.Benchmark;

public static class Program
{
    private record RunInterval(TimeSpan Timing, int FrameCount, int CompileCount, int MethodCallCount);

    public static async Task<int> Main(string[] args)
    {
        var options = CommandLineHandler.Parse(args);
        if (options == null)
        {
            return 1;
        }

        Macro? macro = null;
        if (options.Macro != null)
        {
            using var file = options.Macro.OpenRead();
            macro = await Macro.ParseAsync(file);
        }

        var framesPerInterval = options.FramesPerInterval ?? 60;
        const int runCount = 5;
        var runs = new Queue<RunInterval>[runCount];

        Console.WriteLine($"Starting {runCount} benchmark runs");
        for (var x = 0; x < runCount; x++)
        {
            Queue<RunInterval>? runInfo;
            if (options.NesConfig != null)
            {
                runInfo = RunBenchmark<NesHal>(options, macro);
            }
            else if (options.C64Config != null)
            {
                runInfo = RunBenchmark<C64Hal>(options, macro);
            }
            else
            {
                const string message = "No known benchmark system type provided";
                throw new ArgumentException(message);
            }

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

            await writer.WriteLineAsync("Average ms, Average ms Per Frame, Compilation Count, " +
                                        "Cache Rate, ");

            HashSet<ushort> globalCompiledMethods = [];
            
            // Contents
            for (var x = 0; x < intervalCount; x++)
            {
                await writer.WriteAsync($"{x}, ");
                var compilationCount = 0;
                var frameCount = 0;
                var totalMs = 0.0;
                var totalCompilations = 0;
                var totalMethodCallCount = 0;
                var firstMethodCallCount = 0;
                var prevGlobalCompiledMethodCount = globalCompiledMethods.Count;
                var intervalGlobalCompiledMethodCount = 0;

                for (var y = 0; y < runs.Length; y++)
                {
                    var info = runs[y].Dequeue();
                    if (y == 0)
                    {
                        // The first run needs to write the frame count
                        await writer.WriteAsync($"{info.FrameCount}, ");
                        frameCount = info.FrameCount;
                        compilationCount = info.CompileCount;
                        firstMethodCallCount = info.MethodCallCount;

                        intervalGlobalCompiledMethodCount = globalCompiledMethods.Count - prevGlobalCompiledMethodCount;
                    }

                    await writer.WriteAsync($"{info.Timing.TotalMilliseconds:0.000}, ");
                    totalMs += info.Timing.TotalMilliseconds;
                    totalCompilations += info.CompileCount;
                    totalMethodCallCount += info.MethodCallCount;
                }

                var averagePerRun = totalMs / runs.Length;
                var averagePerFrame = averagePerRun / frameCount;
                var averageCompilations = totalCompilations / runs.Length;
                var averageCallCounts = totalMethodCallCount / runs.Length;
                var cacheRatePercent = ((double)totalMethodCallCount - totalCompilations) / totalMethodCallCount * 100;

                if (Math.Abs(averageCompilations - compilationCount) > 10)
                {
                    Console.WriteLine($"Interval {x} had different compilation counts in different runs! ({averageCompilations} vs {compilationCount})");
                }

                if (Math.Abs(averageCallCounts - firstMethodCallCount) > 10)
                {
                    Console.WriteLine($"Interval {x} had different method call counts for different runs! ({averageCallCounts} vs {firstMethodCallCount})");
                }

                await writer.WriteAsync($"{averagePerRun:0.000}, {averagePerFrame:0.000}, ");
                await writer.WriteAsync($"{compilationCount}, ");
                await writer.WriteAsync($"{cacheRatePercent:0.00}%, ");
                await writer.WriteLineAsync();
            }
        }

        Console.WriteLine($"Benchmark results written to {path}");

        return 0;
    }

    private static Queue<RunInterval>? RunBenchmark<THal>(CommandLineHandler.Options options, Macro? macro)
    where THal : Base6502Hal
    {
        ISystem<THal> system;
        IJitCustomizer jitCustomizer;

        if (typeof(THal) == typeof(NesHal))
        {
            if (options.NesConfig == null)
            {
                throw new ArgumentNullException(nameof(options.NesConfig));
            }

            system = (ISystem<THal>) new NesSystem(options.NesConfig, macro);
            jitCustomizer = new NesJitCustomizer();
        }
        else if (typeof(THal) == typeof(C64Hal))
        {
            if (options.C64Config == null)
            {
                throw new ArgumentNullException(nameof(options.C64Config));
            }

            system = (ISystem<THal>) new C64System(options.C64Config, macro);
            jitCustomizer = new C64JitCustomizer();
        }
        else
        {
            var message = $"No known benchmarking system known for {typeof(THal).FullName}";
            throw new NotSupportedException(message);
        }

        var interpreter = new Ir6502Interpreter();
        jitCustomizer.AddInstructions(interpreter);

        var jitCompiler = new JitCompiler<THal>(system.Hal, jitCustomizer, system.MemoryBus, interpreter)
        {
            AlwaysUseInterpreter = options.UseInterpreter,
        };

        var framesPerInterval = options.FramesPerInterval ?? 60;
        var frameCount = 0;
        var stopwatch = new Stopwatch();
        var timings = new Queue<RunInterval>((int)Math.Ceiling((decimal)options.FrameCount / framesPerInterval));
        var prevCompileCount = 0;
        var prevCallCount = 0;

        system.SetFrameNumber(frameCount);

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

                    var intervalCompileCount = jitCompiler.MethodNotCompiledCount - prevCompileCount;
                    prevCompileCount = jitCompiler.MethodNotCompiledCount;

                    var intervalCallCount = jitCompiler.MethodCallCount - prevCallCount;
                    prevCallCount = jitCompiler.MethodCallCount;

                    timings.Enqueue(new RunInterval(
                        stopwatch.Elapsed,
                        framesPerInterval,
                        intervalCompileCount,
                        intervalCallCount));

                    stopwatch.Restart();
                    isNewInterval = true;
                }

                if (frameCount >= options.FrameCount)
                {
                    stopwatch.Stop();

                    if (!isNewInterval)
                    {
                        var intervalCompileCount = jitCompiler.MethodNotCompiledCount - prevCompileCount;
                        var intervalCallCount = jitCompiler.MethodCallCount - prevCallCount;

                        timings.Enqueue(new RunInterval(
                            stopwatch.Elapsed,
                            frameCount % framesPerInterval,
                            intervalCompileCount,
                            intervalCallCount));
                    }

                    system.CodeCancellationTokenSource.Cancel();
                }

                system.SetFrameNumber(frameCount);
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
