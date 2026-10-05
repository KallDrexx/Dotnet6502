using System;
using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Benchmark;

/// <summary>
/// Represents a type of system that can be benchmarked
/// </summary>
public interface ISystem<out THal> where THal : I6502Hal
{
    public IMemoryBus MemoryBus { get; }
    public CancellationTokenSource CodeCancellationTokenSource { get; }
    public THal Hal { get; }
    public Action? OnFrameFinished { get; set; }

    public ushort GetResetVector();
    public void SetFrameNumber(int frameNumber);
}

