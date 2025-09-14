namespace DotNesJit.Common.Hal;

/// <summary>
/// NES Hardware Abstraction Layer interface
/// </summary>
public interface INesHal
{
    byte ARegister { get; set; }
    byte XRegister { get; set; }
    byte YRegister { get; set; }
    byte ProcessorStatus { get; set; }
    byte StackPointer { get; set; }

    void SetFlag(CpuStatusFlags flag, bool value);
    bool GetFlag(CpuStatusFlags flag);

    byte ReadMemory(ushort address);
    void WriteMemory(ushort address, byte value);

    ushort GetProgramCounter();
    void SetProgramCounter(ushort value);

    void PushToStack(byte value);
    byte PopFromStack();

    public void TriggerSoftwareInterrupt();
    public void JumpToAddress(ushort address);
    public void CallFunction(ushort address);
    public void ReturnFromSubroutine();
    public void ReturnFromInterrupt();
}