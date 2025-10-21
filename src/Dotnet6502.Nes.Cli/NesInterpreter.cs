using System.Dynamic;
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
            if (!_functionCache.TryGetValue((ushort)nextAddress, out var functionInfo))
            {
                var regions = _memoryBus.GetAllCodeRegions();
                var function = FunctionDecompiler.Decompile((ushort)nextAddress, regions);
                var addressIndexMap = function.OrderedInstructions
                    .Select((instruction, index) => new { Instruction = instruction, Index = index })
                    .GroupBy(x => x.Instruction.CPUAddress)
                    .ToDictionary(x => x.Key, x => x.Select(x => x.Index).First());

                functionInfo = new FunctionInfo(function, addressIndexMap);
                _functionCache.Add((ushort)nextAddress, functionInfo);
            }

            // Run the function until we get an address
            nextAddress = ExecuteFunction(functionInfo);
        }
    }

    private int ExecuteFunction(FunctionInfo functionInfo)
    {
        var function = functionInfo.Function;
        if (function.OrderedInstructions.Count == 0)
        {
            var message = $"Function 0x{function.Address:X4} has no instructions";
            throw new InvalidOperationException(message);
        }

        var index = 0;
        while (true)
        {
            var instruction = function.OrderedInstructions[index];
            var pollResult = _hal.PollForInterrupt();
            if (pollResult > 0)
            {
                _hal.PushToStack((byte)(instruction.CPUAddress >> 8));
                _hal.PushToStack((byte)(instruction.CPUAddress & 0xFF));
                var flags = _hal.ProcessorStatus & 0b00110000;
                _hal.PushToStack((byte)(flags));

                var intLow = _hal.ReadMemory(pollResult);
                int intHigh = _hal.ReadMemory((ushort)(pollResult + 1));
                return (ushort)((intHigh << 8) | intLow);
            }

            _hal.DebugHook(instruction.ToString());
            _hal.IncrementCpuCycleCount(instruction.Info.Cycles);
            var nextAddress = instruction.Info.Mnemonic switch
            {
                "ADC" => ExecuteAdc(instruction),
                "AND" => ExecuteAnd(instruction),
                "ASL" => ExecuteAsl(instruction),
                "BCC" => ExecuteBcc(instruction),
                "BCS" => ExecuteBcs(instruction),
                "BEQ" => ExecuteBeq(instruction),
                "BIT" => ExecuteBit(instruction),
                "BMI" => ExecuteBmi(instruction),
                "BNE" => ExecuteBne(instruction),
                "BPL" => ExecuteBpl(instruction),
                "BRK" => ExecuteBrk(instruction),
                "BVC" => ExecuteBvc(instruction),
                "BVS" => ExecuteBvs(instruction),
                "CLC" => ExecuteClc(instruction),
                "CLD" => ExecuteCld(instruction),
                "CLI" => ExecuteCli(instruction),
                "CLV" => ExecuteClv(instruction),
                "CMP" => ExecuteCmp(instruction),
                "CPX" => ExecuteCpx(instruction),
                "CPY" => ExecuteCpy(instruction),
                "DEC" => ExecuteDec(instruction),
                "DEX" => ExecuteDex(instruction),
                "DEY" => ExecuteDey(instruction),
                "EOR" => ExecuteEor(instruction),
                "INC" => ExecuteInc(instruction),
                "INX" => ExecuteInx(instruction),
                "INY" => ExecuteIny(instruction),
                "JMP" => ExecuteJmp(instruction),
                "JSR" => ExecuteJsr(instruction),
                "LDA" => ExecuteLda(instruction),
                "LDX" => ExecuteLdx(instruction),
                "LDY" => ExecuteLdy(instruction),
                "LSR" => ExecuteLsr(instruction),
                "NOP" => ExecuteNop(instruction),
                "ORA" => ExecuteOra(instruction),
                "PHA" => ExecutePha(instruction),
                "PHP" => ExecutePhp(instruction),
                "PLA" => ExecutePla(instruction),
                "PLP" => ExecutePlp(instruction),
                "ROL" => ExecuteRol(instruction),
                "ROR" => ExecuteRor(instruction),
                "RTI" => ExecuteRti(),
                "RTS" => ExecuteRts(),
                "SBC" => ExecuteSbc(instruction),
                "SEC" => ExecuteSec(instruction),
                "SED" => ExecuteSed(instruction),
                "SEI" => ExecuteSei(instruction),
                "STA" => ExecuteSta(instruction),
                "STX" => ExecuteStx(instruction),
                "STY" => ExecuteSty(instruction),
                "TAX" => ExecuteTax(instruction),
                "TAY" => ExecuteTay(instruction),
                "TSX" => ExecuteTsx(instruction),
                "TXA" => ExecuteTxa(instruction),
                "TXS" => ExecuteTxs(instruction),
                "TYA" => ExecuteTya(instruction),
                _ => throw new NotSupportedException(instruction.Info.Mnemonic),
            };

            if (nextAddress != null)
            {
                if (!functionInfo.AddressInstructionIndexes.TryGetValue(nextAddress.Value, out index))
                {
                    return nextAddress.Value;
                }
            }
            else
            {
                index++;
            }
        }
    }

    private ushort? ExecuteAdc(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister + memoryValue + (_hal.GetFlag(CpuStatusFlags.Carry) ? 1 : 0);

        _hal.SetFlag(CpuStatusFlags.Carry, result > 0xFF);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Overflow, ((result ^ _hal.ARegister) & (result ^ memoryValue) & 0x80) == 0x80);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);
        _hal.ARegister = (byte)result;

        return null;
    }

    private ushort? ExecuteAnd(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister & memoryValue;

        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);
        _hal.ARegister = (byte) result;

        return null;
    }

    private ushort? ExecuteAsl(DisassembledInstruction instruction)
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

        return null;
    }

    private ushort? ExecuteBcc(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? null
            : instruction.TargetAddress!.Value;
    }

    private ushort? ExecuteBcs(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? instruction.TargetAddress!.Value
            : null;
    }

    private ushort? ExecuteBeq(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Zero)
            ? instruction.TargetAddress!.Value
            : null;
    }

    private ushort? ExecuteBit(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister & memoryValue);

        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Overflow, (result & 0x40) > 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteBmi(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Negative)
            ? instruction.TargetAddress!.Value
            : null;
    }

    private ushort? ExecuteBne(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Zero)
            ? null
            : instruction.TargetAddress!.Value;
    }

    private ushort? ExecuteBpl(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Negative)
            ? null
            : instruction.TargetAddress!.Value;
    }

    private ushort? ExecuteBrk(DisassembledInstruction instruction)
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

    private ushort? ExecuteBvc(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Carry)
            ? null
            : instruction.TargetAddress!.Value;
    }

    private ushort? ExecuteBvs(DisassembledInstruction instruction)
    {
        return _hal.GetFlag(CpuStatusFlags.Overflow)
            ? instruction.TargetAddress!.Value
            : null;
    }

    private ushort? ExecuteClc(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Carry, false);

        return null;
    }

    private ushort? ExecuteCld(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Decimal, false);

        return null;
    }

    private ushort? ExecuteCli(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.InterruptDisable, false);

        return null;
    }

    private ushort? ExecuteClv(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Overflow, false);

        return null;
    }

    private ushort? ExecuteCmp(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.ARegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteCpx(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.XRegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.XRegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteCpy(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.YRegister - memoryValue;

        _hal.SetFlag(CpuStatusFlags.Carry, _hal.YRegister >= memoryValue);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteDec(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var value = memoryValue - 1;
        SetValueFromAddressingMode(instruction, (byte)value);

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteDex(DisassembledInstruction instruction)
    {
        var value = _hal.XRegister - 1;
        _hal.XRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteDey(DisassembledInstruction instruction)
    {
        var value = _hal.YRegister - 1;
        _hal.YRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteEor(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister ^ memoryValue);

        _hal.ARegister = result;
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteInc(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var value = memoryValue + 1;
        SetValueFromAddressingMode(instruction, (byte)value);

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)(value) == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteInx(DisassembledInstruction instruction)
    {
        var value = _hal.XRegister + 1;
        _hal.XRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)(value) == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteIny(DisassembledInstruction instruction)
    {
        var value = _hal.YRegister + 1;
        _hal.YRegister = (byte)value;

        _hal.SetFlag(CpuStatusFlags.Zero, (byte)value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteJmp(DisassembledInstruction instruction)
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

    private ushort? ExecuteJsr(DisassembledInstruction instruction)
    {
        var nextAddress = (ushort)(instruction.CPUAddress + 2);
        _hal.PushToStack((byte)(nextAddress >> 8));
        _hal.PushToStack((byte)(nextAddress & 0xFF));

        return instruction.TargetAddress!.Value;
    }

    private ushort? ExecuteLda(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.ARegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteLdx(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.XRegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteLdy(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        _hal.YRegister = memoryValue;
        _hal.SetFlag(CpuStatusFlags.Zero, memoryValue == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (memoryValue & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteLsr(DisassembledInstruction instruction)
    {
        var value = GetValueFromAddressingMode(instruction);
        var shifted = (byte)(value >> 1);
        SetValueFromAddressingMode(instruction, shifted);

        _hal.SetFlag(CpuStatusFlags.Carry, (value & 0x01) > 0);
        _hal.SetFlag(CpuStatusFlags.Zero, shifted == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, false);

        return null;
    }

    private ushort? ExecuteNop(DisassembledInstruction instruction)
    {
        return null;
    }

    private ushort? ExecuteOra(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = (byte)(_hal.ARegister | memoryValue);

        _hal.ARegister = result;
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);

        return null;
    }

    private ushort? ExecutePha(DisassembledInstruction instruction)
    {
        _hal.PushToStack(_hal.ARegister);

        return null;
    }

    private ushort? ExecutePhp(DisassembledInstruction instruction)
    {
        var flag = _hal.ProcessorStatus | 0b00110000;
        _hal.PushToStack((byte)flag);

        return null;
    }

    private ushort? ExecutePla(DisassembledInstruction instruction)
    {
        var value = _hal.PopFromStack();
        _hal.ARegister = value;
        _hal.SetFlag(CpuStatusFlags.Zero, value == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (value & 0x80) > 0);

        return null;
    }

    private ushort? ExecutePlp(DisassembledInstruction instruction)
    {
        var value = _hal.PopFromStack();
        _hal.ProcessorStatus = value;

        return null;
    }

    private ushort? ExecuteRol(DisassembledInstruction instruction)
    {
        var value = GetValueFromAddressingMode(instruction);
        var carry = _hal.GetFlag(CpuStatusFlags.Carry) ? 1 : 0;
        var rotated = (byte)((value << 1) | carry);

        SetValueFromAddressingMode(instruction, rotated);
        _hal.SetFlag(CpuStatusFlags.Carry, (value & 0x80) > 0);
        _hal.SetFlag(CpuStatusFlags.Zero, rotated == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (rotated & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteRor(DisassembledInstruction instruction)
    {
        var value = GetValueFromAddressingMode(instruction);
        var carry = _hal.GetFlag(CpuStatusFlags.Carry) ? 0x80 : 0;
        var rotated = (byte)((value >> 1) | carry);

        SetValueFromAddressingMode(instruction, rotated);
        _hal.SetFlag(CpuStatusFlags.Carry, (value & 0x01) > 0);
        _hal.SetFlag(CpuStatusFlags.Zero, rotated == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (rotated & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteRti()
    {
        _hal.ProcessorStatus = _hal.PopFromStack();
        var addressLow = _hal.PopFromStack();
        var addressHigh = _hal.PopFromStack();

        return (ushort)((addressHigh << 8) | addressLow);
    }

    private ushort? ExecuteRts()
    {
        var addressLow = _hal.PopFromStack();
        var addressHigh = _hal.PopFromStack();
        var addressFull = (ushort)((addressHigh << 8) | addressLow);

        return (ushort)(addressFull + 1);
    }

    private ushort? ExecuteSbc(DisassembledInstruction instruction)
    {
        var memoryValue = GetValueFromAddressingMode(instruction);
        var result = _hal.ARegister - memoryValue - ~(_hal.GetFlag(CpuStatusFlags.Carry) ? 1 : 0);
        var aValue = _hal.ARegister;

        _hal.SetFlag(CpuStatusFlags.Carry, result < 0);
        _hal.SetFlag(CpuStatusFlags.Zero, result == 0);
        _hal.SetFlag(CpuStatusFlags.Overflow, ((result ^ aValue) & (result ^ ~memoryValue) & 0x80) > 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (result & 0x80) > 0);
        _hal.ARegister = (byte)result;

        return null;
    }

    private ushort? ExecuteSec(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Carry, true);

        return null;
    }

    private ushort? ExecuteSed(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.Decimal, true);

        return null;
    }

    private ushort? ExecuteSei(DisassembledInstruction instruction)
    {
        _hal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        return null;
    }

    private ushort? ExecuteSta(DisassembledInstruction instruction)
    {
        SetValueFromAddressingMode(instruction, _hal.ARegister);

        return null;
    }

    private ushort? ExecuteStx(DisassembledInstruction instruction)
    {
        SetValueFromAddressingMode(instruction, _hal.XRegister);

        return null;
    }

    private ushort? ExecuteSty(DisassembledInstruction instruction)
    {
        SetValueFromAddressingMode(instruction, _hal.YRegister);

        return null;
    }

    private ushort? ExecuteTax(DisassembledInstruction instruction)
    {
        _hal.XRegister = _hal.ARegister;

        _hal.SetFlag(CpuStatusFlags.Zero, _hal.XRegister == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.XRegister & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteTay(DisassembledInstruction instruction)
    {
        _hal.YRegister = _hal.ARegister;

        _hal.SetFlag(CpuStatusFlags.Zero, _hal.YRegister == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.YRegister & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteTsx(DisassembledInstruction instruction)
    {
        _hal.XRegister = _hal.StackPointer;

        _hal.SetFlag(CpuStatusFlags.Zero, _hal.XRegister == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.XRegister & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteTxa(DisassembledInstruction instruction)
    {
        _hal.ARegister = _hal.XRegister;
        _hal.SetFlag(CpuStatusFlags.Zero, _hal.ARegister == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.ARegister & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteTxs(DisassembledInstruction instruction)
    {
        _hal.StackPointer = _hal.XRegister;
        _hal.SetFlag(CpuStatusFlags.Zero, _hal.StackPointer == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.StackPointer & 0x80) > 0);

        return null;
    }

    private ushort? ExecuteTya(DisassembledInstruction instruction)
    {
        _hal.ARegister = _hal.YRegister;
        _hal.SetFlag(CpuStatusFlags.Zero, _hal.ARegister == 0);
        _hal.SetFlag(CpuStatusFlags.Negative, (_hal.ARegister & 0x80) > 0);

        return null;
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
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.Accumulator:
                _hal.ARegister = value;
                break;

            case AddressingMode.ZeroPage:
            {
                var address = instruction.Bytes![1];
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.ZeroPageX:
            {
                var address = (byte)(instruction.Bytes![1] + _hal.XRegister);
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.ZeroPageY:
            {
                var address = (byte)(instruction.Bytes![1] + _hal.YRegister);
                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.Absolute:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]);
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.AbsoluteX:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]) + _hal.XRegister;
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.AbsoluteY:
            {
                var address = ((instruction.Bytes![2] << 8) | instruction.Bytes[1]) + _hal.YRegister;
                _hal.WriteMemory((ushort) address, value);
                break;
            }

            case AddressingMode.IndexedIndirect:
            {
                var lowByteAddress = (byte)(instruction.Bytes![1] + _hal.XRegister);
                var highByteAddress = (byte)(lowByteAddress + 1);

                var lowByte = _hal.ReadMemory(lowByteAddress);
                var highByte = _hal.ReadMemory(highByteAddress);
                var address = (ushort)((highByte << 8) | lowByte);

                _hal.WriteMemory(address, value);
                break;
            }

            case AddressingMode.IndirectIndexed:
            {
                var lowByteAddress = instruction.Bytes![1];
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
}