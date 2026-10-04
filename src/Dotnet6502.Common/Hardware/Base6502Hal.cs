namespace Dotnet6502.Common.Hardware;

public class Base6502Hal
{
    /// <summary>
    /// A delegate that allows the JIT to be notified of memory being changed. A value of
    /// true being returned means that the currently executing function has had its instructions
    /// modified.
    /// </summary>
    public delegate bool MemoryWriteEvent(ushort address);

    private readonly MemoryBus _memoryBus;
    private byte _flags;
    private bool _recompilationRequired;

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }
    public byte StackPointer { get; set; } = 0xFF;
    public ushort CurrentInstructionAddress { get; set; }
    public MemoryWriteEvent? OnMemoryWritten;

    public byte ProcessorStatus
    {
        get
        {
            // Ensure Unused flag always comes back as true
            SetFlag(CpuStatusFlags.Unused, true);
            return _flags;
        }

        set => _flags = value;
    }

    private ushort StackAddress => (ushort)(0x0100 | StackPointer);

    public Base6502Hal(MemoryBus memoryBus)
    {
        _memoryBus = memoryBus;
    }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        if (flag == CpuStatusFlags.Unused && !value)
        {
            return; // never let unused be set to false
        }
        
        var shift = GetShiftAmount(flag);
        var mask = (byte)(1 << shift);
        if (value)
        {
            _flags |= mask;
        }
        else
        {
            _flags &= (byte)~mask;
        }
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        var shift = GetShiftAmount(flag);
        var mask = (byte)(1 << shift);

        return (_flags & mask) > 0;
    }

    public virtual byte ReadMemory(ushort address)
    {
        return _memoryBus.Read(address);
    }

    public virtual void WriteMemory(ushort address, byte value)
    {
        _memoryBus.Write(address, value);
        if (OnMemoryWritten?.Invoke(address) == true)
        {
            // Only reset via a poll or a new function call
            _recompilationRequired = true;
        }
    }

    public virtual void PushToStack(byte value)
    {
        _memoryBus.Write(StackAddress, value);
        StackPointer--;
    }

    public virtual byte PopFromStack()
    {
        if (StackPointer == byte.MaxValue)
        {
            throw new InvalidOperationException("Stack pointer overflowed");
        }

        StackPointer++;
        var value = _memoryBus.Read(StackAddress);

        return value;
    }

    /// <summary>
    /// Polls for a waiting interrupt. If an interrupt is pending, the address of the interrupt pointer
    /// is returned. If no interrupt is pending, then Zero is returned.
    /// </summary>
    public virtual ushort PollForInterrupt()
    {
        return 0;
    }

    public bool PollForRecompilation()
    {
        var previous = _recompilationRequired;
        _recompilationRequired = false;

        return previous;
    }

    public virtual void DebugHook(string info)
    {
    }
    
    private static byte GetShiftAmount(CpuStatusFlags flag)
    {
        return flag switch
        {
            CpuStatusFlags.Carry => 0,
            CpuStatusFlags.Zero => 1,
            CpuStatusFlags.InterruptDisable => 2,
            CpuStatusFlags.Decimal => 3,
            CpuStatusFlags.BFlag => 4,
            CpuStatusFlags.Unused => 5,
            CpuStatusFlags.Overflow => 6,
            CpuStatusFlags.Negative => 7,
            _ => throw new NotSupportedException(flag.ToString()),
        };
    }
}
