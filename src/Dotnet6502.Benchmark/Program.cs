using System.Diagnostics;
using Dotnet6502.Benchmark;
using Dotnet6502.Common.Compilation;
using Dotnet6502.Nes;

var options = CommandLineHandler.Parse(args);
if (options == null)
{
    return 1;
}

ISystem system;
if (options.NesConfig != null)
{
    system = new NesSystem(options.NesConfig);
}
else
{
    Console.Error.WriteLine("No known system specified to benchmark");
    return 1;
}

var jitCustomizer = new NesJitCustomizer();
var interpreter = new Ir6502Interpreter();
jitCustomizer.AddInstructions(interpreter);

var jitCompiler = new JitCompiler(system.Hal, jitCustomizer, system.MemoryBus, interpreter);

var framesPerInterval = options.FramesPerInterval ?? 60;
long frameCount = 0;
var stopwatch = new Stopwatch();
var timings = new Queue<(TimeSpan Timing, int FrameCount)>((int)Math.Ceiling((decimal)options.FrameCount / framesPerInterval));

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
            timings.Enqueue((stopwatch.Elapsed, framesPerInterval));
            stopwatch.Restart();
            isNewInterval = true;
        }

        if (frameCount >= options.FrameCount)
        {
            stopwatch.Stop();
            if (!isNewInterval)
            {
                timings.Enqueue((stopwatch.Elapsed, (int)frameCount % framesPerInterval));
            }
            
            system.CodeCancellationTokenSource.Cancel();
        }
    }
};

Console.WriteLine($"Starting benchmark");
var resetVector = system.GetResetVector();

try
{
    jitCompiler.RunMethod(resetVector);
}
catch (TaskCanceledException)
{
    // Expected
}

Console.WriteLine($"Benchmark finished");
Console.WriteLine($"Frames Executed: {frameCount}");
Console.WriteLine($"Frames Per Interval: {framesPerInterval}");
Console.WriteLine();

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

