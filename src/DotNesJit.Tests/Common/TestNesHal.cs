using DotNesJit.Common;
using DotNesJit.Common.Hal;

namespace DotNesJit.Tests.Common;

public class TestNesHal : INesHal
{
    public Dictionary<CpuStatusFlags, bool> Flags { get; } = new();
    public Dictionary<ushort, byte> MemoryValues { get; } = new();
    public byte StackPointer { get; set; }
    public ushort ProgramCounter { get; set; }

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }
    public byte ProcessorStatus { get; set; }

    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        Flags[flag] = value;
    }

    public bool GetFlag(CpuStatusFlags flag)
    {
        return Flags.GetValueOrDefault(flag);
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

    public void TriggerSoftwareInterrupt()
    {
        throw new NotImplementedException();
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