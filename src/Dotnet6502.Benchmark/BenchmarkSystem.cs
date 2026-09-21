using System;
using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Benchmark;

/// <summary>
/// Represents a type of system that can be benchmarked
/// </summary>
public abstract class BenchmarkingSystem
{
    protected readonly int _framesPerTimeLog;
    
    protected BenchmarkingSystem(int framesPerTimeLog)
    {
        _framesPerTimeLog = framesPerTimeLog;
    }
    
    public virtual MemoryBus MemoryBus { get; protected set; }
    public virtual CancellationTokenSource CodeCancellationTokenSource { get; set; }
    public virtual Base6502Hal Hal { get; set; }
    public virtual Queue<TimeSpan> LoggedTimings { get; set; }
}

