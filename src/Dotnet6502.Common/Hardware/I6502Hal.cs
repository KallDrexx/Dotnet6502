using System;

namespace Dotnet6502.Common.Hardware;

/// <summary>
/// Represents a 6502 hardware abstraction layer object
/// </summary>
public interface I6502Hal
{
    /// <summary>
    /// A delegate that allows the JIT to be notified of memory being changed. A value of
    /// true being returned means that the currently executing function has had its instructions
    /// modified.
    /// </summary>
    public delegate bool MemoryWriteEvent(ushort address);
    
    byte ARegister { get; set; }
    byte XRegister { get; set; }
    byte YRegister { get; set; }
    byte StackPointer { get; set; }
    ushort CurrentInstructionAddress { get; set; }
    byte ProcessorStatus { get; set; }

    void SetFlag(CpuStatusFlags flag, bool value);
    bool GetFlag(CpuStatusFlags flag);

    byte ReadMemory(ushort address);
    void WriteMemory(ushort address, byte value);
    void PushToStack(byte value);
    byte PopFromStack();

    /// <summary>
    /// Polls for a waiting interrupt. If an interrupt is pending, the address of the interrupt pointer
    /// is returned. If no interrupt is pending, then Zero is returned.
    /// </summary>
    ushort PollForInterrupt();

    bool PollForRecompilation();
    void DebugHook(string info);

    MemoryWriteEvent? OnMemoryWritten { get; set; }
}
