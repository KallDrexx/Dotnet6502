namespace Dotnet6502.Common;

/// <summary>
/// 6502 Processor Hardware Abstraction Layer interface
/// </summary>
public interface I6502Hal
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

    void PushToStack(byte value);
    byte PopFromStack();

    public void TriggerSoftwareInterrupt();
}