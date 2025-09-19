using Dotnet6502.Common;
using Dotnet6502.Common.Hal;

namespace Dotnet6502.Tests.Common;

public class Test6502Hal : I6502Hal
{
    private readonly Stack<byte> _stack = new();

    public Dictionary<CpuStatusFlags, bool> Flags { get; } = new()
    {
        { CpuStatusFlags.Always1, true },
        { CpuStatusFlags.BFlag, false },
        { CpuStatusFlags.Carry, false },
        { CpuStatusFlags.Decimal, false },
        { CpuStatusFlags.InterruptDisable, false },
        { CpuStatusFlags.Negative, false },
        { CpuStatusFlags.Overflow, false },
        { CpuStatusFlags.Zero, false },
    };

    public Dictionary<ushort, byte> MemoryValues { get; } = new();
    public byte StackPointer { get; set; }
    public ushort ProgramCounter { get; set; }

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }

    public byte ProcessorStatus
    {
        get => (byte)(
            (Convert.ToByte(Flags[CpuStatusFlags.Negative]) << 7) |
            (Convert.ToByte(Flags[CpuStatusFlags.Overflow]) << 6) |
            (Convert.ToByte(Flags[CpuStatusFlags.Always1]) << 5) |
            (Convert.ToByte(Flags[CpuStatusFlags.BFlag]) << 4) |
            (Convert.ToByte(Flags[CpuStatusFlags.Decimal]) << 3) |
            (Convert.ToByte(Flags[CpuStatusFlags.InterruptDisable]) << 2) |
            (Convert.ToByte(Flags[CpuStatusFlags.Zero]) << 1) |
            (Convert.ToByte(Flags[CpuStatusFlags.Carry]) << 0));
        set
        {
            Flags[CpuStatusFlags.Negative] =         (value & 0b10000000) == 0b10000000;
            Flags[CpuStatusFlags.Overflow] =         (value & 0b01000000) == 0b01000000;
            Flags[CpuStatusFlags.Always1] =          (value & 0b00100000) == 0b00100000;
            Flags[CpuStatusFlags.BFlag] =            (value & 0b00010000) == 0b00010000;
            Flags[CpuStatusFlags.Decimal] =          (value & 0b00001000) == 0b00001000;
            Flags[CpuStatusFlags.InterruptDisable] = (value & 0b00000100) == 0b00000100;
            Flags[CpuStatusFlags.Zero] =             (value & 0b00000010) == 0b00000010;
            Flags[CpuStatusFlags.Carry] =            (value & 0b00000001) == 0b00000001;
        }
    }

    public bool SoftwareInterruptTriggered { get; private set; }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        Flags[flag] = value;
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        return Flags[flag];
    }

    public byte GetStackPointer()
    {
        return StackPointer;
    }

    public void SetStackPointer(byte value)
    {
        StackPointer = value;
    }

    public byte ReadMemory(ushort address)
    {
        return MemoryValues.GetValueOrDefault(address);
    }

    public void WriteMemory(ushort address, byte value)
    {
        MemoryValues[address] = value;
    }

    public ushort GetProgramCounter()
    {
        return ProgramCounter;
    }

    public void SetProgramCounter(ushort value)
    {
        ProgramCounter = value;
    }

    public void PushToStack(byte value)
    {
        _stack.Push(value);
    }

    public byte PopFromStack()
    {
        return _stack.Pop();
    }

    public void TriggerSoftwareInterrupt()
    {
        SoftwareInterruptTriggered = true;
    }

    public void JumpToAddress(ushort address)
    {
        throw new NotImplementedException();
    }

    public void CallFunction(ushort address)
    {
        throw new NotImplementedException();
    }

    public void ReturnFromSubroutine()
    {
        throw new NotImplementedException();
    }

    public void ReturnFromInterrupt()
    {
        throw new NotImplementedException();
    }
}