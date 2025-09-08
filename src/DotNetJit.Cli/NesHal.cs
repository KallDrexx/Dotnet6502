namespace DotNetJit.Cli;

/// <summary>
/// Hardware abstraction layer that provides hooks that the NES op codes utilize to perform operations,
/// such as setting status flags or performing memory mapping as required.
/// </summary>
public class NesHal
{
    private readonly byte[] _memory = new byte[0x10000]; // 64KB of memory

    private readonly Dictionary<CpuStatusFlags, bool> _statusFlags = new()
    {
        { CpuStatusFlags.Carry, false },
        { CpuStatusFlags.Zero, false },
        { CpuStatusFlags.InterruptDisable, false },
        { CpuStatusFlags.Decimal, false },
        { CpuStatusFlags.BFlag, false },
        { CpuStatusFlags.Always1, true },
        { CpuStatusFlags.Overflow, false },
        { CpuStatusFlags.Negative, false },
    };

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        if (!Enum.GetValues<CpuStatusFlags>().Contains(flag))
        {
            throw new NotSupportedException(flag.ToString());
        }

        _statusFlags[flag] = value;
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        if (!Enum.GetValues<CpuStatusFlags>().Contains(flag))
        {
            throw new NotSupportedException(flag.ToString());
        }

        return _statusFlags[flag];
    }

    public byte ReadMemory(ushort address)
    {
        // TODO: Implement proper memory mapping
        return _memory[address];
    }

    public void WriteMemory(ushort address, byte value)
    {
        _memory[address] = value;
    }
}