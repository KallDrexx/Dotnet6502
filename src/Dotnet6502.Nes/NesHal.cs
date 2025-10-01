using Dotnet6502.Common;

namespace Dotnet6502.Nes;

public class NesHal : I6502Hal
{
    private readonly Dictionary<CpuStatusFlags, bool> _flags  = new()
    {
        { CpuStatusFlags.Always1, false }, // fceux seems to always have this as false
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
    private readonly StreamWriter? _debugWriter;
    private long _cpuCycleCount;
    private long _instructionCount;

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

    public NesHal(NesMemory memory, Ppu ppu, StreamWriter? debugWriter, CancellationToken cancellationToken)
    {
        _memory = memory;
        _ppu = ppu;
        _cancellationToken = cancellationToken;
        _debugWriter = debugWriter;
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

        _debugWriter?.WriteLine($"Read 0x{readValue:X2} from address 0x{address:X4}");
        return readValue;
    }

    public void WriteMemory(ushort address, byte value)
    {
        _debugWriter?.WriteLine($"Writing 0x{value:X2} to address 0x{address:X4}");
        _memory.Write(address, value);
    }

    public void PushToStack(byte value)
    {
        var stackAddress = (ushort)(0x0100 | StackPointer);

        _debugWriter?.WriteLine($"Pushing 0x{value:X2} to stack address 0x{stackAddress:X4}");
        _memory.Write(stackAddress, value);
        StackPointer--;
    }

    public byte PopFromStack()
    {
        var stackAddress = (ushort)(0x0100 | StackPointer);
        var value = _memory.Read(stackAddress);
        _debugWriter?.WriteLine($"popped 0x{value:X2} from stack address 0x{stackAddress:X4}");
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

        _cpuCycleCount += count;
        _instructionCount++;
        var nmiTriggered = _ppu.RunNextStep(count);
        if (nmiTriggered)
        {
            if (NmiHandler != null)
            {
                _debugWriter?.WriteLine("----Entering NMI");
                NmiHandler();
                _debugWriter?.WriteLine("----Exiting NMI");
            }
            else
            {
                throw new InvalidOperationException("NMI triggered but no NMI handler defined");
            }
        }
    }

    public void DebugHook(string info)
    {
        _debugWriter?.Write($"{info} - A:{ARegister:X2} X:{XRegister:X2} Y:{YRegister:X2} P:{ProcessorStatus:X2} ");
        _debugWriter?.Write($"SP:{StackPointer:X2} ");
        _debugWriter?.Write($"PPUCTL:{_ppu.PpuCtrl.ToByte():X2} PPUSTATUS:{_ppu.PpuStatus.ToByte():X2} ");
        _debugWriter?.WriteLine($"PPUADDR:{_ppu.PpuAddr:X4} ({_cpuCycleCount:N0} cycles, {_instructionCount:N0} inst)");
    }
}