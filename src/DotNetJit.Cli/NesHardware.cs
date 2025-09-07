namespace DotNetJit.Cli;

public class NesHardware
{
    public void SetFlag(CpuStatusFlags flag, bool value)
    {

    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        return false;
    }

    public byte ReadMemory(ushort address)
    {
        return 0;
    }

    public void WriteMemory(ushort address, byte value)
    {

    }
}