namespace DotNesJit.Common.Hal;

/// <summary>
/// NES Hardware Abstraction Layer interface
/// </summary>
public interface INesHal
{
    void SetFlag(CpuStatusFlags flag, bool value);
    bool GetFlag(CpuStatusFlags flag);
    byte GetProcessorStatus();
    void SetProcessorStatus(byte status);

    byte GetStackPointer();
    void SetStackPointer(byte value);

    byte ReadMemory(ushort address);
    void WriteMemory(ushort address, byte value);

    ushort GetProgramCounter();
    void SetProgramCounter(ushort value);

    public void TriggerSoftwareInterrupt();
    public void JumpToAddress(ushort address);
    public void CallFunction(ushort address);
    public void ReturnFromSubroutine();
    public void ReturnFromInterrupt();
}