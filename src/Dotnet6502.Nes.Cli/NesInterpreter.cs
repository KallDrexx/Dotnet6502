using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Nes.Cli;

/// <summary>
/// Basic 6502 interpreter to test speed vs JIT
/// </summary>
public class NesInterpreter
{
    private record FunctionInfo(DecompiledFunction Function, IReadOnlyDictionary<ushort, int> AddressInstructionIndexes);

    private readonly NesHal _hal;
    private readonly MemoryBus _memoryBus;
    private readonly Dictionary<ushort, FunctionInfo> _functionCache = new();

    public NesInterpreter(NesHal hal, MemoryBus memoryBus)
    {
        _hal = hal;
        _memoryBus = memoryBus;
    }

    public void RunFunction(ushort address)
    {
        int nextAddress = address;
        while (nextAddress >= 0)
        {
            if (!_functionCache.TryGetValue(address, out var functionInfo))
            {
                var regions = _memoryBus.GetAllCodeRegions();
                var function = FunctionDecompiler.Decompile(address, regions);
                var addressIndexMap = function.OrderedInstructions
                    .Select((instruction, index) => new { Instruction = instruction, Index = index })
                    .ToDictionary(x => x.Instruction.CPUAddress, x => x.Index);

                functionInfo = new FunctionInfo(function, addressIndexMap);
                _functionCache.Add(address, functionInfo);
            }

            // Run the function until we get an address
            nextAddress = ExecuteFunction(functionInfo);
        }
    }

    private int ExecuteFunction(FunctionInfo functionInfo)
    {
        var function = functionInfo.Function;
        var currentAddress = function.OrderedInstructions[0].CPUAddress;
        while (true)
        {
            if (!functionInfo.AddressInstructionIndexes.TryGetValue(currentAddress, out var index))
            {
                // Address is not in this function
                return currentAddress;
            }

            var instruction = function.OrderedInstructions[index];
            currentAddress = instruction.Info.Mnemonic switch
            {
                "ADC" => ExecuteAdc(instruction),
                _ => throw new NotSupportedException(instruction.Info.Mnemonic),
            };
        }
    }

    private ushort ExecuteAdc(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister + memoryValue + (_hal.GetFlag(CpuStatusFlags.Carry) ? 1 : 0);

        _hal.SetFlag(CpuStatusFlags.Carry, result > 0xFF);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Overflow, ((result ^ _hal.ARegister) & (result ^ memoryValue) & 0x80) == 0x80);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);
        _hal.ARegister = (byte)result;

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteAnd(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister & memoryValue;

        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);
        _hal.ARegister = (byte) result;

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteAsl(DisassembledInstruction instruction)
    {
        int newValue;
        if (instruction.Info.AddressingMode == AddressingMode.Accumulator)
        {
            var value = _hal.ARegister;
            newValue = value << 1;
            _hal.ARegister = (byte)newValue;
        }
        else
        {
            var value = GetValueFromAddressingMode(instruction);
            newValue = value << 1;
            SetValueFromAddressingMode(instruction, value);
        }

        _hal.SetFlag(CpuStatusFlags.Carry, (newValue & 0x100) > 0);
        _hal.SetFlag(CpuStatusFlags.Zero, (newValue & 0xFF) == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (newValue & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteBcc(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? NextInstructionAddress(instruction)
            : instruction.TargetAddress!.Value;
    }

    private ushort ExecuteBcs(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? instruction.TargetAddress!.Value
            : NextInstructionAddress(instruction);
    }

    private ushort ExecuteBeq(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Zero)
            ? instruction.TargetAddress!.Value
            : NextInstructionAddress(instruction);
    }

    private ushort ExecuteBit(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister & memoryValue);

        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Overflow, (result & 0x40) > 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteBmi(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Negative)
            ? instruction.TargetAddress!.Value
            : NextInstructionAddress(instruction);
    }

    private ushort ExecuteBne(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Zero)
            ? NextInstructionAddress(instruction)
            : instruction.TargetAddress!.Value;
    }

    private ushort ExecuteBpl(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Negative)
            ? NextInstructionAddress(instruction)
            : instruction.TargetAddress!.Value;
    }

    private ushort ExecuteBrk(DisassembledInstruction instruction)
    {
        var nextAddress = (ushort)(instruction.CPUAddress + 2);
        _hal.PushToStack((byte)(nextAddress >> 8));
        _hal.PushToStack((byte)(nextAddress & 0xFF));

        var flags = _hal.ProcessorStatus | 0b00110000;
        _hal.PushToStack((byte)flags);

        var interruptHigh = _hal.ReadMemory(0xFFFF);
        var interruptLow = _hal.ReadMemory(0xFFFE);

        return (ushort)((interruptHigh << 8) | interruptLow);
    }

    private ushort ExecuteBvc(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? NextInstructionAddress(instruction)
            : instruction.TargetAddress!.Value;
    }

    private ushort ExecuteBvs(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Overflow)
            ? instruction.TargetAddress!.Value
            : NextInstructionAddress(instruction);
    }

    private ushort ExecuteClc(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Carry, false);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteCld(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Decimal, false);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteCli(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.InterruptDisable, false);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteClv(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Overflow, false);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteCmp(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.ARegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteCpx(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.XRegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.XRegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteCpy(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.YRegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.YRegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteDec(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var value = memoryValue - 1;
        SetValueFromAddressingMode(instruction, (byte)value);

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteDex(DisassembledInstruction instruction)
    {
        var value = _hal.XRegister - 1;
        _hal.XRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteDey(DisassembledInstruction instruction)
    {
        var value = _hal.YRegister - 1;
        _hal.YRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteEor(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister ^ memoryValue);

        _hal.ARegister = result;
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteInc(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var value = memoryValue + 1;
        SetValueFromAddressingMode(instruction, (byte)value);

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)(value) == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteInx(DisassembledInstruction instruction)
    {
        var value = _hal.XRegister + 1;
        _hal.XRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)(value) == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteIny(DisassembledInstruction instruction)
    {
        var value = _hal.YRegister + 1;
        _hal.YRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteJmp(DisassembledInstruction instruction)
    {
        if (instruction.Info.AddressingMode == AddressingMode.Absolute)
        {
            return instruction.TargetAddress!.Value;
        }

        // Indirect jump
        var lookupAddress = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]);
        var lookupHigh = lookupAddress + 1;

        if ((lookupHigh & 0xFF) == 0)
        {
            lookupHigh = lookupAddress & 0xFF00;
        }

        var jumpLow = _hal.ReadMemory((ushort)lookupAddress);
        var jumpHigh = _hal.ReadMemory((ushort)(lookupHigh));

        return (ushort)((jumpHigh << 8) | jumpLow);
    }

    private ushort ExecuteJsr(DisassembledInstruction instruction)
    {
        var nextAddress = (ushort)(instruction.CPUAddress + 2);
        _hal.PushToStack((byte)(nextAddress >> 8));
        _hal.PushToStack((byte)(nextAddress & 0xFF));

        return instruction.TargetAddress!.Value;
    }

    private ushort ExecuteLda(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.ARegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteLdx(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.XRegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteLdy(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.YRegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteLsr(DisassembledInstruction instruction)
    {
        var value = GetValueFromAddressingMode(instruction);
        var shifted = (byte)(value >> 1);
        SetValueFromAddressingMode(instruction, shifted);

        _hal.SetFlag(CpuStatusFlags.Carry, (value & 0x01) > 0);
        _hal.SetFlag(CpuStatusFlags.Zero, shifted == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, false);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteNop(DisassembledInstruction instruction)
    {
        return NextInstructionAddress(instruction);
    }

    private ushort ExecuteOra(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister | memoryValue);

        _hal.ARegister = result;
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecutePha(DisassembledInstruction instruction)
    {
        _hal.PushToStack(_hal.ARegister);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecutePhp(DisassembledInstruction instruction)
    {
        var flag = _hal.ProcessorStatus | 0b00110000;
        _hal.PushToStack((byte)flag);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecutePla(DisassembledInstruction instruction)
    {
        var value = _hal.PopFromStack();
        _hal.ARegister = value;
        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return NextInstructionAddress(instruction);
    }

    private ushort ExecutePlp(DisassembledInstruction instruction)
    {
        var value = _hal.PopFromStack();
        _hal.ProcessorStatus = value;

        return NextInstructionAddress(instruction);
    }

    private byte GetValueFromAddressingMode(DisassembledInstruction instruction)
    {
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.Accumulator:
                return _hal.ARegister;

            case AddressingMode.Immediate:
                return instruction.Bytes![1];

            case AddressingMode.ZeroPage:
            {
                var address = instruction.Bytes![1];
                return _hal.ReadMemory(address);
            }

            case AddressingMode.ZeroPageX:
            {
                var address = (byte)(instruction.Bytes![1] + _hal.XRegister);
                return _hal.ReadMemory(address);
            }

            case AddressingMode.ZeroPageY:
            {
                var address = (byte)(instruction.Bytes![1] + _hal.YRegister);
                return _hal.ReadMemory(address);
            }

            case AddressingMode.Absolute:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]);
                return _hal.ReadMemory((ushort) address);
            }

            case AddressingMode.AbsoluteX:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]) + _hal.XRegister;
                return _hal.ReadMemory((ushort) address);
            }

            case AddressingMode.AbsoluteY:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]) + _hal.YRegister;
                return _hal.ReadMemory((ushort) address);
            }

            case AddressingMode.IndexedIndirect:
            {
                var lowByteAddress = (byte)(instruction.Bytes![1] + _hal.XRegister);
                var highByteAddress = (byte)(lowByteAddress + 1);

                var lowByte = _hal.ReadMemory(lowByteAddress);
                var highByte = _hal.ReadMemory(highByteAddress);
                var address = (ushort)((highByte << 8) | lowByte);

                return _hal.ReadMemory(address);
            }

            case AddressingMode.IndirectIndexed:
            {
                var lowByteAddress = instruction.Bytes![1];
                var highByteAddress = (byte)(lowByteAddress + 1);

                var lowByte = _hal.ReadMemory(lowByteAddress);
                var highByte = _hal.ReadMemory(highByteAddress);
                var address = ((highByte << 8) | lowByte) + _hal.YRegister;

                return _hal.ReadMemory((ushort)address);
            }

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }
    }

    private void SetValueFromAddressingMode(DisassembledInstruction instruction, byte value)
    {
        if (instruction.Bytes == null)
        {
            throw new NullReferenceException(nameof(instruction.Bytes));
        }

        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.ZeroPage:
            {
                var address = instruction.Bytes[1];
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.ZeroPageX:
            {
                var address = (byte)(instruction.Bytes[1] + _hal.XRegister);
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.ZeroPageY:
            {
                var address = (byte)(instruction.Bytes[1] + _hal.YRegister);
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.Absolute:
            {
                var address = ((instruction.Bytes[2] << 8) | instruction.Bytes[1]);
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.AbsoluteX:
            {
                var address = ((instruction.Bytes[2] << 8) | instruction.Bytes[1]) + _hal.XRegister;
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.AbsoluteY:
            {
                var address = ((instruction.Bytes[2] << 8) | instruction.Bytes[1]) + _hal.YRegister;
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.IndexedIndirect:
            {
                var lowByteAddress = (byte)(instruction.Bytes[1] + _hal.XRegister);
                var highByteAddress = (byte)(lowByteAddress + 1);

                var lowByte = _hal.ReadMemory(lowByteAddress);
                var highByte = _hal.ReadMemory(highByteAddress);
                var address = (ushort)((highByte << 8) | lowByte);

                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.IndirectIndexed:
            {
                var lowByteAddress = (byte)(instruction.Bytes[1]);
                var highByteAddress = (byte)(lowByteAddress + 1);

                var lowByte = _hal.ReadMemory(lowByteAddress);
                var highByte = _hal.ReadMemory(highByteAddress);
                var address = ((highByte << 8) | lowByte) + _hal.YRegister;

                _hal.WriteMemory((ushort)address, value);
                break;
            }

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }
    }

    private static ushort NextInstructionAddress(DisassembledInstruction instruction) =>
        (ushort)(instruction.CPUAddress + instruction.Info.Cycles);
}