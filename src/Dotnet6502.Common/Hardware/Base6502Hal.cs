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
    private readonly Dictionary<CpuStatusFlags, bool> _flags  = new()
    {
        { CpuStatusFlags.Unused, true },
        { CpuStatusFlags.BFlag, false },
        { CpuStatusFlags.Carry, false },
        { CpuStatusFlags.Decimal, false },
        { CpuStatusFlags.InterruptDisable, false },
        { CpuStatusFlags.Negative, false },
        { CpuStatusFlags.Overflow, false },
        { CpuStatusFlags.Zero, false },
    };

    private bool _recompilationRequired;

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }
    public byte StackPointer { get; set; } = 0xFF;
    public MemoryWriteEvent? OnMemoryWritten;

    public byte ProcessorStatus
    {
        get => (byte)(
            (Convert.ToByte(_flags[CpuStatusFlags.Negative]) << 7) |
            (Convert.ToByte(_flags[CpuStatusFlags.Overflow]) << 6) |
            (Convert.ToByte(_flags[CpuStatusFlags.Unused]) << 5) |
            (Convert.ToByte(_flags[CpuStatusFlags.BFlag]) << 4) |
            (Convert.ToByte(_flags[CpuStatusFlags.Decimal]) << 3) |
            (Convert.ToByte(_flags[CpuStatusFlags.InterruptDisable]) << 2) |
            (Convert.ToByte(_flags[CpuStatusFlags.Zero]) << 1) |
            (Convert.ToByte(_flags[CpuStatusFlags.Carry]) << 0));
        set
        {
            _flags[CpuStatusFlags.Negative] =         (value & 0b10000000) == 0b10000000;
            _flags[CpuStatusFlags.Overflow] =         (value & 0b01000000) == 0b01000000;
            _flags[CpuStatusFlags.Decimal] =          (value & 0b00001000) == 0b00001000;
            _flags[CpuStatusFlags.InterruptDisable] = (value & 0b00000100) == 0b00000100;
            _flags[CpuStatusFlags.Zero] =             (value & 0b00000010) == 0b00000010;
            _flags[CpuStatusFlags.Carry] =            (value & 0b00000001) == 0b00000001;
        }
    }

    private ushort StackAddress => (ushort)(0x0100 | StackPointer);

    public Base6502Hal(MemoryBus memoryBus)
    {
        _memoryBus = memoryBus;
    }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        _flags[flag] = value;
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        return _flags[flag];
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
}