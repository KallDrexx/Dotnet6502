
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
var resetVector = system.GetResetVector();

var frameCount = 0;
var stopwatch = new Stopwatch();
var timeSoFar = TimeSpan.Zero;

system.OnFrameFinished = () =>
{
    if (!stopwatch.IsRunning)
    {
        // First frame is missed for accuracy
        stopwatch.Start();
    }
    else
    {
        stopwatch.Stop();
        timeSoFar += stopwatch.Elapsed;
        
    }
};

Console.WriteLine($"Starting benchmark");




return 0;

