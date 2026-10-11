using System.Diagnostics;
using System.Text.Json;
using Dotnet6502.C64.Integration;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Common.Macros;
using Dotnet6502.Nes;

namespace Dotnet6502.Benchmark;

public static class Program
{
    private record TimingStats(double Average, double Median, double StdDev, double Min, double Max)
    {
        public static TimingStats Calculate(IReadOnlyList<double> values)
        {
            var sorted = values.Order().ToArray();
            var average = sorted.Average();
            var middle = sorted.Length / 2;
            var median = sorted.Length % 2 == 0
                ? (sorted[middle - 1] + sorted[middle]) / 2
                : sorted[middle];

            // Sample standard deviation, since the runs are a sample of possible run timings
            var stdDev = sorted.Length > 1
                ? Math.Sqrt(sorted.Sum(x => (x - average) * (x - average)) / (sorted.Length - 1))
                : 0;

            return new TimingStats(average, median, stdDev, sorted[0], sorted[^1]);
        }
    }

    private record RunInterval(TimeSpan Timing, int FrameCount, int CompileCount, int MethodCallCount);

    /// <summary>
    /// Hidden argument that tells the process to execute a single benchmark run and write
    /// the interval results to the specified file. Each run is executed in its own process
    /// so that every run starts with a cold .NET JIT, just like the emulator does.
    /// </summary>
    private const string ChildResultFlag = "--child-result";

    public static async Task<int> Main(string[] args)
    {
        var childFlagIndex = Array.IndexOf(args, ChildResultFlag);
        if (childFlagIndex >= 0)
        {
            if (childFlagIndex + 1 >= args.Length)
            {
                Console.Error.WriteLine($"Error: {ChildResultFlag} requires a file path");
                return 1;
            }

            var resultPath = args[childFlagIndex + 1];
            var benchmarkArgs = args
                .Where((_, index) => index != childFlagIndex && index != childFlagIndex + 1)
                .ToArray();

            return await RunChildAsync(benchmarkArgs, resultPath);
        }

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
            Console.WriteLine($"Starting run {x + 1} of {runCount}");
            var runInfo = await RunChildProcessAsync(args);
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

            await writer.WriteLineAsync("Average ms, Median ms, Std Dev ms, Min ms, Max ms, " +
                                        "Average ms Per Frame, Median ms Per Frame, Compilation Count, " +
                                        "Cache Rate, ");

            var runTotalMs = new double[runs.Length];
            var totalFrameCount = 0;
            var totalCompilationCount = 0;
            var totalMethodCallCountAllRuns = 0;
            var totalCompilationsAllRuns = 0;

            HashSet<ushort> globalCompiledMethods = [];
            
            // Contents
            for (var x = 0; x < intervalCount; x++)
            {
                await writer.WriteAsync($"{x}, ");
                var compilationCount = 0;
                var frameCount = 0;
                var timings = new double[runs.Length];
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
                    timings[y] = info.Timing.TotalMilliseconds;
                    runTotalMs[y] += info.Timing.TotalMilliseconds;
                    totalCompilations += info.CompileCount;
                    totalMethodCallCount += info.MethodCallCount;
                }

                var stats = TimingStats.Calculate(timings);
                var averageCompilations = (double)totalCompilations / runs.Length;
                var averageCallCounts = (double)totalMethodCallCount / runs.Length;
                var cacheRatePercent = ((double)totalMethodCallCount - totalCompilations) / totalMethodCallCount * 100;

                if (Math.Abs(averageCompilations - compilationCount) > 10)
                {
                    Console.WriteLine($"Interval {x} had different compilation counts in different runs! ({averageCompilations} vs {compilationCount})");
                }

                if (Math.Abs(averageCallCounts - firstMethodCallCount) > 10)
                {
                    Console.WriteLine($"Interval {x} had different method call counts for different runs! ({averageCallCounts} vs {firstMethodCallCount})");
                }

                await WriteStatsAsync(writer, stats, frameCount);
                await writer.WriteAsync($"{compilationCount}, ");
                await writer.WriteAsync($"{cacheRatePercent:0.00}%, ");
                await writer.WriteLineAsync();

                totalFrameCount += frameCount;
                totalCompilationCount += compilationCount;
                totalMethodCallCountAllRuns += totalMethodCallCount;
                totalCompilationsAllRuns += totalCompilations;
            }

            // Whole run totals, so the spread of entire runs can be compared
            var totalStats = TimingStats.Calculate(runTotalMs);
            var totalCacheRatePercent = ((double)totalMethodCallCountAllRuns - totalCompilationsAllRuns) /
                                        totalMethodCallCountAllRuns * 100;

            await writer.WriteAsync($"Total, {totalFrameCount}, ");
            foreach (var runMs in runTotalMs)
            {
                await writer.WriteAsync($"{runMs:0.000}, ");
            }

            await WriteStatsAsync(writer, totalStats, totalFrameCount);
            await writer.WriteAsync($"{totalCompilationCount}, ");
            await writer.WriteAsync($"{totalCacheRatePercent:0.00}%, ");
            await writer.WriteLineAsync();

            Console.WriteLine($"Total run time: median {totalStats.Median:0.000} ms, " +
                              $"std dev {totalStats.StdDev:0.000} ms, " +
                              $"min {totalStats.Min:0.000} ms, " +
                              $"max {totalStats.Max:0.000} ms");
        }

        Console.WriteLine($"Benchmark results written to {path}");

        return 0;
    }

    private static async Task WriteStatsAsync(StreamWriter writer, TimingStats stats, int frameCount)
    {
        await writer.WriteAsync($"{stats.Average:0.000}, {stats.Median:0.000}, {stats.StdDev:0.000}, ");
        await writer.WriteAsync($"{stats.Min:0.000}, {stats.Max:0.000}, ");
        await writer.WriteAsync($"{stats.Average / frameCount:0.000}, {stats.Median / frameCount:0.000}, ");
    }

    /// <summary>
    /// Executes a single benchmark run in the current process and writes the results to the
    /// specified file for the parent process to read.
    /// </summary>
    private static async Task<int> RunChildAsync(string[] args, string resultPath)
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

        var runInfo = RunBenchmark(options, macro);

        if (runInfo == null)
        {
            return 1;
        }

        await File.WriteAllTextAsync(resultPath, JsonSerializer.Serialize(runInfo.ToArray()));
        return 0;
    }

    /// <summary>
    /// Executes a single benchmark run in a new process, so it does not benefit from any .NET
    /// JIT tiering done by previous runs.
    /// </summary>
    private static async Task<Queue<RunInterval>?> RunChildProcessAsync(string[] args)
    {
        var resultPath = Path.Combine(Path.GetTempPath(), $"dn6502-run-{Guid.NewGuid():N}.json");
        var processPath = Environment.ProcessPath
                          ?? throw new InvalidOperationException("Unable to determine the current process path");

        var startInfo = new ProcessStartInfo(processPath)
        {
            UseShellExecute = false,
        };

        // When launched via `dotnet run` or `dotnet <dll>`, the process is the dotnet host, so
        // the benchmark assembly needs to be passed to it.
        if (Path.GetFileNameWithoutExtension(processPath) == "dotnet")
        {
            startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        }

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        startInfo.ArgumentList.Add(ChildResultFlag);
        startInfo.ArgumentList.Add(resultPath);

        try
        {
            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException("Failed to start benchmark child process");

            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                Console.Error.WriteLine($"Benchmark child process exited with code {process.ExitCode}");
                return null;
            }

            var json = await File.ReadAllTextAsync(resultPath);
            var intervals = JsonSerializer.Deserialize<RunInterval[]>(json);
            if (intervals == null)
            {
                Console.Error.WriteLine("Benchmark child process did not produce any results");
                return null;
            }

            return new Queue<RunInterval>(intervals);
        }
        finally
        {
            File.Delete(resultPath);
        }
    }

    private static Queue<RunInterval>? RunBenchmark(CommandLineHandler.Options options, Macro? macro)
    {
        ISystem system;
        IJitCustomizer jitCustomizer;
        if (options.NesConfig != null)
        {
            system = new NesSystem(options.NesConfig, macro);
            jitCustomizer = new NesJitCustomizer();
        }
        else if (options.C64Config != null)
        {
            system = new C64System(options.C64Config, macro);
            jitCustomizer = new C64JitCustomizer();
        }
        else
        {
            Console.Error.WriteLine("No known system specified to benchmark");
            return null;
        }

        var interpreter = new Ir6502Interpreter();
        jitCustomizer.AddInstructions(interpreter);

        var jitCompiler = new JitCompiler(system.Hal, jitCustomizer, system.MemoryBus, interpreter)
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
