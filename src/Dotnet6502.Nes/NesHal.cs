using System.Reflection;
using Dotnet6502.Common;
using NESDecompiler.Core.CPU;

namespace Dotnet6502.Nes;

public class NesHal : I6502Hal
{
    private readonly Dictionary<CpuStatusFlags, bool> _flags  = new()
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

    private readonly NesMemory _memory;
    private readonly Ppu _ppu;
    private readonly CancellationToken _cancellationToken;

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }
    public byte StackPointer { get; set; }
    public Action? NmiHandler { get; set; }

    public byte ProcessorStatus
    {
        get => (byte)(
            (Convert.ToByte(_flags[CpuStatusFlags.Negative]) << 7) |
            (Convert.ToByte(_flags[CpuStatusFlags.Overflow]) << 6) |
            (Convert.ToByte(_flags[CpuStatusFlags.Always1]) << 5) |
            (Convert.ToByte(_flags[CpuStatusFlags.BFlag]) << 4) |
            (Convert.ToByte(_flags[CpuStatusFlags.Decimal]) << 3) |
            (Convert.ToByte(_flags[CpuStatusFlags.InterruptDisable]) << 2) |
            (Convert.ToByte(_flags[CpuStatusFlags.Zero]) << 1) |
            (Convert.ToByte(_flags[CpuStatusFlags.Carry]) << 0));
        set
        {
            _flags[CpuStatusFlags.Negative] =         (value & 0b10000000) == 0b10000000;
            _flags[CpuStatusFlags.Overflow] =         (value & 0b01000000) == 0b01000000;
            _flags[CpuStatusFlags.Always1] =          (value & 0b00100000) == 0b00100000;
            _flags[CpuStatusFlags.BFlag] =            (value & 0b00010000) == 0b00010000;
            _flags[CpuStatusFlags.Decimal] =          (value & 0b00001000) == 0b00001000;
            _flags[CpuStatusFlags.InterruptDisable] = (value & 0b00000100) == 0b00000100;
            _flags[CpuStatusFlags.Zero] =             (value & 0b00000010) == 0b00000010;
            _flags[CpuStatusFlags.Carry] =            (value & 0b00000001) == 0b00000001;
        }
    }

    public NesHal(NesMemory memory, Ppu ppu, CancellationToken cancellationToken)
    {
        _memory = memory;
        _ppu = ppu;
        _cancellationToken = cancellationToken;
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
        var readValue = _memory.Read(address);
        return readValue;
    }

    public void WriteMemory(ushort address, byte value)
    {
        _memory.Write(address, value);
    }

    public void PushToStack(byte value)
    {
        var stackAddress = (ushort)(0x0100 | StackPointer);
        _memory.Write(stackAddress, value);
        StackPointer--;
    }

    public byte PopFromStack()
    {
        var stackAddress = (ushort)(0x0100 | StackPointer);
        var value = _memory.Read(stackAddress);
        StackPointer++;

        return value;
    }

    public void TriggerSoftwareInterrupt()
    {
        throw new NotImplementedException();
    }

    public void IncrementCpuCycleCount(int count)
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            throw new TaskCanceledException("Cancellation requested");
        }

        var nmiTriggered = _ppu.RunNextStep(count);
        if (nmiTriggered)
        {
            if (NmiHandler != null)
            {
                NmiHandler();
            }
            else
            {
                throw new InvalidOperationException("NMI triggered but no NMI handler defined");
            }
        }
    }
}