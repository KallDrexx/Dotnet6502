namespace Dotnet6502.Common;

public class Base6502Hal
{
    private readonly IMemoryMap _memoryMap;
    private readonly Dictionary<CpuStatusFlags, bool> _flags  = new()
    {
        { CpuStatusFlags.Unused, false },
        { CpuStatusFlags.BFlag, false },
        { CpuStatusFlags.Carry, false },
        { CpuStatusFlags.Decimal, false },
        { CpuStatusFlags.InterruptDisable, false },
        { CpuStatusFlags.Negative, false },
        { CpuStatusFlags.Overflow, false },
        { CpuStatusFlags.Zero, false },
    };

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }
    public byte StackPointer { get; set; } = 0xFF;

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

    public Base6502Hal(IMemoryMap memoryMap)
    {
        _memoryMap = memoryMap;
    }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        _flags[flag] = value;
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        return _flags[flag];
    }

    public byte ReadMemory(ushort address)
    {
        return _memoryMap.Read(address);
    }

    public void WriteMemory(ushort address, byte value)
    {
        _memoryMap.Write(address, value);
    }

    public void PushToStack(byte value)
    {
        _memoryMap.Write(StackAddress, value);
        StackPointer--;
    }

    public byte PopFromStack()
    {
        if (StackPointer == byte.MaxValue)
        {
            throw new InvalidOperationException("Stack pointer overflowed");
        }

        StackPointer++;
        var value = _memoryMap.Read(StackAddress);

        return value;
    }

    public void TriggerSoftwareInterrupt()
    {
        throw new NotImplementedException();
    }
}