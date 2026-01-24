using Dotnet6502.Common.Hardware;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Executes Ir6502 instructions without generating MSIL, used for self-modifying functions.
/// </summary>
public class Ir6502Interpreter
{
    public delegate void CustomInstructionHandler(Ir6502.Instruction instruction, Base6502Hal hal, int[] locals);

    private readonly Dictionary<Type, CustomInstructionHandler> _customHandlers = new();

    public void AddHandler<T>(CustomInstructionHandler handler) where T : Ir6502.Instruction
    {
        _customHandlers.Add(typeof(T), handler);
    }

    public ExecutableMethod CreateExecutableMethod(IReadOnlyList<ConvertedInstruction> instructions)
    {
        var flattenedInstructions = instructions
            .SelectMany(x => x.Ir6502Instructions.Select(y => new { Orig = x.OriginalInstruction, Ir = y }))
            .Select(x => new KeyValuePair<Ir6502.Instruction, DisassembledInstruction>(x.Ir, x.Orig))
            .ToArray();

        var labelTargets = BuildLabelTargets(flattenedInstructions);
        var localCount = GetMaxLocalCount(flattenedInstructions);

        return hal => Execute(flattenedInstructions, labelTargets, localCount, hal);
    }

    private int Execute(
        IReadOnlyList<KeyValuePair<Ir6502.Instruction, DisassembledInstruction>> instructions,
        IReadOnlyDictionary<Ir6502.Identifier, int> labelTargets,
        int localCount,
        Base6502Hal hal)
    {
        var locals = new int[localCount];
        var instructionPointer = 0;

        while (instructionPointer < instructions.Count)
        {
            var instructionKvp = instructions[instructionPointer];
            var instruction = instructionKvp.Key;

            // First check custom instructions
            if (_customHandlers.TryGetValue(instruction.GetType(), out var handler))
            {
                handler(instruction, hal, locals);
            }
            else
            {
                switch (instruction)
                {
                    case Ir6502.Binary binary:
                        ExecuteBinary(binary, hal, locals);
                        break;

                    case Ir6502.CallFunction callFunction:
                        return ResolveCallTarget(callFunction.CallTarget, hal, locals);

                    case Ir6502.ConvertVariableToByte convert:
                        locals[convert.Variable.Index] = (byte)locals[convert.Variable.Index];
                        break;

                    case Ir6502.Copy copy:
                        var value = ReadValue(copy.Source, hal, locals);
                        WriteValue(copy.Destination, value, hal, locals);
                        break;

                    case Ir6502.DebugValue debugValue:
                        hal.DebugHook(
                            $"{debugValue.ValueToLog} value: {ReadValue(debugValue.ValueToLog, hal, locals)}");
                        break;

                    case Ir6502.Jump jump:
                        instructionPointer = ResolveLabel(jump.Target, labelTargets);
                        continue;

                    case Ir6502.JumpIfNotZero jump:
                        if (ReadValue(jump.Condition, hal, locals) != 0)
                        {
                            instructionPointer = ResolveLabel(jump.Target, labelTargets);
                            continue;
                        }

                        break;

                    case Ir6502.JumpIfZero jump:
                        if (ReadValue(jump.Condition, hal, locals) == 0)
                        {
                            instructionPointer = ResolveLabel(jump.Target, labelTargets);
                            continue;
                        }

                        break;

                    case Ir6502.Label:
                        break;

                    case Ir6502.NoOp:
                        break;

                    case Ir6502.PopStackValue pop:
                        WriteValue(pop.Destination, hal.PopFromStack(), hal, locals);
                        break;

                    case Ir6502.PushStackValue push:
                        hal.PushToStack((byte)ReadValue(push.Source, hal, locals));
                        break;

                    case Ir6502.Return returnInstruction:
                        return ReadValue(returnInstruction.VariableWithReturnAddress, hal, locals);

                    case Ir6502.StoreDebugString debugString:
                        hal.DebugHook(debugString.Text);
                        break;

                    case Ir6502.Unary unary:
                        ExecuteUnary(unary, hal, locals);
                        break;

                    case Ir6502.PollForInterrupt poll:
                        var nextAddress = ExecutePollForInterrupt(poll, hal);
                        if (nextAddress != 0)
                        {
                            return nextAddress;
                        }

                        break;

                    case Ir6502.PollForRecompilation:
                        if (hal.PollForRecompilation())
                        {
                            // Return the next instruction to start recompilation from
                            var originalInstruction = instructionKvp.Value;
                            return originalInstruction.CPUAddress + originalInstruction.Info.Size;
                        }
                        break;

                    case Ir6502.RecordCurrentInstructionAddress record:
                        hal.CurrentInstructionAddress = record.Address;
                        break;

                    default:
                        throw new NotSupportedException(instruction.GetType().FullName);
                }
            }

            instructionPointer++;
        }

        return -1;
    }

    private static int ResolveLabel(Ir6502.Identifier target, IReadOnlyDictionary<Ir6502.Identifier, int> labelTargets)
    {
        if (!labelTargets.TryGetValue(target, out var targetIndex))
        {
            throw new InvalidOperationException($"Jump received to target '{target}' but no label exists for that.");
        }

        return targetIndex;
    }

    private static int ExecutePollForInterrupt(Ir6502.PollForInterrupt poll, Base6502Hal hal)
    {
        var interruptVectorAddress = hal.PollForInterrupt();
        if (interruptVectorAddress == 0)
        {
            return 0;
        }

        hal.PushToStack((byte)((poll.ContinuationAddress & 0xFF00) >> 8));
        hal.PushToStack((byte)(poll.ContinuationAddress & 0x00FF));
        hal.PushToStack(hal.ProcessorStatus);

        hal.DebugHook($"Saving 0x{poll.ContinuationAddress:X4} as address on stack");
        hal.SetFlag(CpuStatusFlags.InterruptDisable, true);

        var lowByte = hal.ReadMemory(interruptVectorAddress);
        var highByte = hal.ReadMemory((ushort)(interruptVectorAddress + 1));

        return (highByte << 8) | lowByte;
    }

    private static void ExecuteBinary(Ir6502.Binary binary, Base6502Hal hal, int[] locals)
    {
        var left = ReadValue(binary.Left, hal, locals);
        var right = ReadValue(binary.Right, hal, locals);
        var result = binary.Operator switch
        {
            Ir6502.BinaryOperator.Add => left + right,
            Ir6502.BinaryOperator.And => left & right,
            Ir6502.BinaryOperator.Equals => left == right ? 1 : 0,
            Ir6502.BinaryOperator.GreaterThan => left > right ? 1 : 0,
            Ir6502.BinaryOperator.GreaterThanOrEqualTo => left >= right ? 1 : 0,
            Ir6502.BinaryOperator.LessThan => left < right ? 1 : 0,
            Ir6502.BinaryOperator.LessThanOrEqualTo => left <= right ? 1 : 0,
            Ir6502.BinaryOperator.NotEquals => left != right ? 1 : 0,
            Ir6502.BinaryOperator.Or => left | right,
            Ir6502.BinaryOperator.ShiftLeft => left << right,
            Ir6502.BinaryOperator.ShiftRight => left >> right,
            Ir6502.BinaryOperator.Subtract => left - right,
            Ir6502.BinaryOperator.Xor => left ^ right,
            _ => throw new NotSupportedException(binary.Operator.ToString()),
        };

        WriteValue(binary.Destination, result, hal, locals);
    }

    private static void ExecuteUnary(Ir6502.Unary unary, Base6502Hal hal, int[] locals)
    {
        var source = ReadValue(unary.Source, hal, locals);
        var result = unary.Operator switch
        {
            Ir6502.UnaryOperator.BitwiseNot => ~source,
            Ir6502.UnaryOperator.LogicalNot => source == 0 ? 1 : 0,
            _ => throw new NotSupportedException(unary.Operator.ToString()),
        };

        WriteValue(unary.Destination, result, hal, locals);
    }

    private static int ReadValue(Ir6502.Value value, Base6502Hal hal, int[] locals)
    {
        return value switch
        {
            Ir6502.AllFlags => hal.ProcessorStatus,
            Ir6502.Constant constant => constant.Number,
            Ir6502.Flag flag => hal.GetFlag(ConvertFlagName(flag.FlagName)) ? 1 : 0,
            Ir6502.Memory memory => hal.ReadMemory(GetMemoryAddress(memory, hal)),
            Ir6502.IndirectMemory indirectMemory => ReadIndirectMemory(indirectMemory, hal),
            Ir6502.Register register => register.Name switch
            {
                Ir6502.RegisterName.Accumulator => hal.ARegister,
                Ir6502.RegisterName.XIndex => hal.XRegister,
                Ir6502.RegisterName.YIndex => hal.YRegister,
                _ => throw new NotSupportedException(register.Name.ToString()),
            },
            Ir6502.StackPointer => hal.StackPointer,
            Ir6502.Variable variable => locals[variable.Index],
            _ => throw new NotSupportedException(value.GetType().FullName),
        };
    }

    private static void WriteValue(Ir6502.Value destination, int value, Base6502Hal hal, int[] locals)
    {
        switch (destination)
        {
            case Ir6502.AllFlags:
                hal.ProcessorStatus = (byte)value;
                break;

            case Ir6502.Flag flag:
                hal.SetFlag(ConvertFlagName(flag.FlagName), value != 0);
                break;

            case Ir6502.Memory memory:
                hal.WriteMemory(GetMemoryAddress(memory, hal), (byte)value);
                break;

            case Ir6502.IndirectMemory indirectMemory:
                WriteIndirectMemory(indirectMemory, (byte)value, hal);
                break;

            case Ir6502.Register register:
                switch (register.Name)
                {
                    case Ir6502.RegisterName.Accumulator:
                        hal.ARegister = (byte)value;
                        break;
                    case Ir6502.RegisterName.XIndex:
                        hal.XRegister = (byte)value;
                        break;
                    case Ir6502.RegisterName.YIndex:
                        hal.YRegister = (byte)value;
                        break;
                    default:
                        throw new NotSupportedException(register.Name.ToString());
                }
                break;

            case Ir6502.StackPointer:
                hal.StackPointer = (byte)value;
                break;

            case Ir6502.Variable variable:
                locals[variable.Index] = value;
                break;

            case Ir6502.Constant:
                throw new InvalidOperationException("Can't write to constant");

            default:
                throw new NotSupportedException(destination.GetType().FullName);
        }
    }

    private static ushort GetMemoryAddress(Ir6502.Memory memory, Base6502Hal hal)
    {
        var address = memory.Address;

        if (memory.RegisterToAdd != null)
        {
            var registerValue = memory.RegisterToAdd.Value switch
            {
                Ir6502.RegisterName.XIndex => hal.XRegister,
                Ir6502.RegisterName.YIndex => hal.YRegister,
                _ => throw new NotSupportedException(memory.RegisterToAdd.Value.ToString()),
            };
            address = (ushort)(address + registerValue);
            if (memory.SingleByteAddress)
            {
                address = (byte)address;
            }
        }

        return address;
    }

    private static int ReadIndirectMemory(Ir6502.IndirectMemory indirectMemory, Base6502Hal hal)
    {
        var zeroPageAddress = indirectMemory.ZeroPageAddress;

        if (indirectMemory.IsPreIndexed)
        {
            zeroPageAddress = (byte)(zeroPageAddress + hal.XRegister);
        }

        var lowByte = hal.ReadMemory(zeroPageAddress);
        var highByte = hal.ReadMemory((byte)(zeroPageAddress + 1));
        var address = (ushort)((highByte << 8) | lowByte);

        if (indirectMemory.IsPostIndexed)
        {
            address = (ushort)(address + hal.YRegister);
        }

        return hal.ReadMemory(address);
    }

    private static void WriteIndirectMemory(Ir6502.IndirectMemory indirectMemory, byte value, Base6502Hal hal)
    {
        var zeroPageAddress = indirectMemory.ZeroPageAddress;

        if (indirectMemory.IsPreIndexed)
        {
            zeroPageAddress = (byte)(zeroPageAddress + hal.XRegister);
        }

        var lowByte = hal.ReadMemory(zeroPageAddress);
        var highByte = hal.ReadMemory((byte)(zeroPageAddress + 1));
        var address = (ushort)((highByte << 8) | lowByte);

        if (indirectMemory.IsPostIndexed)
        {
            address = (ushort)(address + hal.YRegister);
        }

        hal.WriteMemory(address, value);
    }

    private static int ResolveCallTarget(Ir6502.ICallTarget callTarget, Base6502Hal hal, int[] locals)
    {
        return callTarget switch
        {
            Ir6502.FunctionAddress functionAddress => ResolveFunctionAddress(functionAddress, hal),
            Ir6502.Variable variable => locals[variable.Index],
            _ => throw new NotSupportedException(callTarget.GetType().ToString()),
        };
    }

    private static int ResolveFunctionAddress(Ir6502.FunctionAddress functionAddress, Base6502Hal hal)
    {
        if (!functionAddress.IsIndirect)
        {
            return functionAddress.Address;
        }

        var lowByte = hal.ReadMemory(functionAddress.Address);
        var highByteAddress = (functionAddress.Address & 0x00FF) == 0x00FF
            ? (ushort)(functionAddress.Address & 0xFF00)
            : (ushort)(functionAddress.Address + 1);
        var highByte = hal.ReadMemory(highByteAddress);

        return (highByte << 8) | lowByte;
    }

    private static IReadOnlyDictionary<Ir6502.Identifier, int> BuildLabelTargets(
        IReadOnlyList<KeyValuePair<Ir6502.Instruction, DisassembledInstruction>> instructions)
    {
        var labels = new Dictionary<Ir6502.Identifier, int>();
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].Key is Ir6502.Label label)
            {
                labels[label.Name] = i;
            }
        }

        return labels;
    }

    private static int GetMaxLocalCount(IReadOnlyList<KeyValuePair<Ir6502.Instruction, DisassembledInstruction>> instructions)
    {
        var largestLocalCount = 0;
        foreach (var kvp in instructions)
        {
            var instruction = kvp.Key;
            var valueProperties = instruction.GetType()
                .GetProperties()
                .Where(x => x.PropertyType == typeof(Ir6502.Value))
                .ToArray();

            foreach (var property in valueProperties)
            {
                if (property.GetValue(instruction) is Ir6502.Variable variable)
                {
                    var variableCount = variable.Index + 1;
                    if (largestLocalCount < variableCount)
                    {
                        largestLocalCount = variableCount;
                    }
                }
            }
        }

        return largestLocalCount;
    }

    private static CpuStatusFlags ConvertFlagName(Ir6502.FlagName flagName)
    {
        return flagName switch
        {
            Ir6502.FlagName.Carry => CpuStatusFlags.Carry,
            Ir6502.FlagName.Zero => CpuStatusFlags.Zero,
            Ir6502.FlagName.InterruptDisable => CpuStatusFlags.InterruptDisable,
            Ir6502.FlagName.BFlag => CpuStatusFlags.BFlag,
            Ir6502.FlagName.Decimal => CpuStatusFlags.Decimal,
            Ir6502.FlagName.Overflow => CpuStatusFlags.Overflow,
            Ir6502.FlagName.Negative => CpuStatusFlags.Negative,
            _ => throw new NotSupportedException(flagName.ToString())
        };
    }
}