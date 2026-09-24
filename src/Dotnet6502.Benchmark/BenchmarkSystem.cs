using System;
using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Benchmark;

/// <summary>
/// Represents a type of system that can be benchmarked
/// </summary>
public interface ISystem
{
    public MemoryBus MemoryBus { get; }
    public CancellationTokenSource CodeCancellationTokenSource { get; }
    public Base6502Hal Hal { get; }
    public Action? OnFrameFinished { get; set; }

    public ushort GetResetVector();
}

