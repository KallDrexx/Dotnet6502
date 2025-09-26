using System.Reflection;
using System.Reflection.Emit;

namespace Dotnet6502.Common;

/// <summary>
/// Generates MSIL for a NES Intermediary Representation instruction
/// </summary>
public class MsilGenerator
{
    public delegate void CustomIlGenerator(Ir6502.Instruction instruction, Context context);

    public record Context(
        ILGenerator IlGenerator,
        FieldInfo HardwareField,
        Func<string, MethodInfo?> GetMethodInfo);

    private readonly Dictionary<Type, CustomIlGenerator> _customIlGenerators;

    /// <summary>
    /// The number of locals that are required for MsilGenerator operations. It is assumed that
    /// the method the ILGenerator is tied to already has this number of locals PLUS the locals
    /// required by the instructions used in the method. This means that any instruction's local
    /// index must be incremented by this amount.
    /// </summary>
    public const int TemporaryLocalsRequired = 3;

    private readonly IReadOnlyDictionary<Ir6502.Identifier, Label> _labels;

    public MsilGenerator(
        IReadOnlyDictionary<Ir6502.Identifier, Label> labels,
        Dictionary<Type, CustomIlGenerator> customIlGenerators)
    {
        _labels = labels;
        _customIlGenerators = customIlGenerators;
    }

    public void Generate(Ir6502.Instruction instruction, Context context)
    {
        if (_customIlGenerators.TryGetValue(instruction.GetType(), out var generator))
        {
            generator.Invoke(instruction, context);
            return;
        }
        
        switch (instruction)
        {
            case Ir6502.Binary binary:
                GenerateBinary(binary, context);
                break;

            case Ir6502.CallFunction callFunction:
                GenerateCallFunction(callFunction, context);
                break;

            case Ir6502.ConvertVariableToByte convertVariableToByte:
                GenerateConvertToByte(convertVariableToByte, context);
                break;

            case Ir6502.Copy copy:
                GenerateCopy(copy, context);
                break;

            case Ir6502.InvokeSoftwareInterrupt:
                GenerateInvokeIrq(context);
                break;

            case Ir6502.Jump jump:
                GenerateJump(jump, context);
                break;

            case Ir6502.JumpIfNotZero jump:
                GenerateJumpIfNotZero(jump, context);
                break;

            case Ir6502.JumpIfZero jump:
                GenerateJumpIfZero(jump, context);
                break;

            case Ir6502.Label label:
                GenerateLabel(label, context);
                break;

            case Ir6502.PopStackValue pop:
                GeneratePopStackValue(pop, context);
                break;

            case Ir6502.PushStackValue push:
                GeneratePushStackValue(push, context);
                break;

            case Ir6502.Return:
                GenerateReturn(context);
                break;

            case Ir6502.Unary unary:
                GenerateUnary(unary, context);
                break;

            default:
                throw new NotSupportedException(instruction.GetType().FullName);
        }
    }

    private static void GenerateBinary(Ir6502.Binary binary, Context context)
    {
        LoadValueToStack(binary.Left, context);
        LoadValueToStack(binary.Right, context);
        EmitBinaryOperator();
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(binary.Destination, context);
        return;

        void EmitBinaryOperator()
        {
            switch (binary.Operator)
            {
                case Ir6502.BinaryOperator.Add:
                    context.IlGenerator.Emit(OpCodes.Add);
                    break;

                case Ir6502.BinaryOperator.And:
                    context.IlGenerator.Emit(OpCodes.And);
                    break;

                case Ir6502.BinaryOperator.Equals:
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.GreaterThan:
                    context.IlGenerator.Emit(OpCodes.Cgt);
                    break;

                case Ir6502.BinaryOperator.GreaterThanOrEqualTo:
                    context.IlGenerator.Emit(OpCodes.Clt);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.LessThan:
                    context.IlGenerator.Emit(OpCodes.Clt);
                    break;

                case Ir6502.BinaryOperator.LessThanOrEqualTo:
                    context.IlGenerator.Emit(OpCodes.Cgt);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.NotEquals:
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.Or:
                    context.IlGenerator.Emit(OpCodes.Or);
                    break;

                case Ir6502.BinaryOperator.ShiftLeft:
                    context.IlGenerator.Emit(OpCodes.Shl);
                    break;
            
                case Ir6502.BinaryOperator.ShiftRight:
                    context.IlGenerator.Emit(OpCodes.Shr);
                    break;
            
                case Ir6502.BinaryOperator.Subtract:
                    context.IlGenerator.Emit(OpCodes.Sub);
                    break;
            
                case Ir6502.BinaryOperator.Xor:
                    context.IlGenerator.Emit(OpCodes.Xor);
                    break;
            
                default:
                    throw new NotSupportedException(binary.Operator.ToString());
            }
        }
    }

    private static void GenerateCallFunction(Ir6502.CallFunction callFunction, Context context)
    {
        var methodInfo = context.GetMethodInfo(callFunction.Name.Characters);
        if (methodInfo == null)
        {
            var message = $"No known method with the name '{callFunction.Name.Characters}' exists";
            throw new InvalidOperationException(message);
        }

        if (methodInfo.IsStatic)
        {
            context.IlGenerator.Emit(OpCodes.Call, methodInfo);
        }
        else
        {
            context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
            context.IlGenerator.Emit(OpCodes.Callvirt, methodInfo);
        }
    }

    private static void GenerateCopy(Ir6502.Copy copy, Context context)
    {
        LoadValueToStack(copy.Source, context);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(copy.Destination, context);
    }

    private static void GenerateInvokeIrq(Context context)
    {
        var invokeMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.TriggerSoftwareInterrupt))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, invokeMethod);
    }

    private void GenerateJump(Ir6502.Jump jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        context.IlGenerator.Emit(OpCodes.Br, label);
    }

    private void GenerateJumpIfZero(Ir6502.JumpIfZero jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, context);
        context.IlGenerator.Emit(OpCodes.Brfalse, label);
    }

    private void GenerateJumpIfNotZero(Ir6502.JumpIfNotZero jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if not zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, context);
        context.IlGenerator.Emit(OpCodes.Brtrue, label);
    }

    private void GenerateLabel(Ir6502.Label label, Context context)
    {
        // This does not define the label, but instead marks the label at the current spot. This
        // means the label must already have been defined.
        if (!_labels.TryGetValue(label.Name, out var ilLabel))
        {
            var message = $"Attempted to mark label position '{label.Name.Characters}' but that label has not" +
                          $"been defined";
            throw new InvalidOperationException(message);
        }

        context.IlGenerator.MarkLabel(ilLabel);
    }

    private static void GeneratePopStackValue(Ir6502.PopStackValue pop, Context context)
    {
        var popMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.PopFromStack))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, popMethod);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(pop.Destination, context);
    }

    private static void GeneratePushStackValue(Ir6502.PushStackValue push, Context context)
    {
        var pushMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.PushToStack))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        LoadValueToStack(push.Source, context);
        context.IlGenerator.Emit(OpCodes.Callvirt, pushMethod);
    }

    private static void GenerateReturn(Context context)
    {
        context.IlGenerator.Emit(OpCodes.Ret);
    }

    private static void GenerateUnary(Ir6502.Unary unary, Context context)
    {
        LoadValueToStack(unary.Source, context);

        switch (unary.Operator)
        {
            case Ir6502.UnaryOperator.BitwiseNot:
                context.IlGenerator.Emit(OpCodes.Not);
                break;
            
            default:
                throw new NotSupportedException(unary.Operator.ToString());
        }
        
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(unary.Destination, context);
    }

    private static void GenerateConvertToByte(Ir6502.ConvertVariableToByte convertVariableToByte, Context context)
    {
        LoadValueToStack(convertVariableToByte.Variable, context);
        context.IlGenerator.Emit(OpCodes.Conv_U1);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(convertVariableToByte.Variable, context);
    }

    private static void LoadValueToStack(Ir6502.Value value, Context context)
    {
        switch (value)
        {
            case Ir6502.AllFlags:
                var getStatusMethod = typeof(I6502Hal)
                    .GetProperty(nameof(I6502Hal.ProcessorStatus))!
                    .GetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
                break;

            case Ir6502.Constant constant:
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)constant.Number);
                break;

            case Ir6502.Flag flag:
                var getFlagMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.GetFlag))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                context.IlGenerator.Emit(OpCodes.Callvirt, getFlagMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;

            case Ir6502.Memory memory:
            {
                var readMemoryMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.ReadMemory))!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                    if (memory.SingleByteAddress)
                    {
                        context.IlGenerator.Emit(OpCodes.Conv_U1);
                    }
                }

                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;
            }

            case Ir6502.IndirectMemory indirectMemory:
            {
                var readMemoryMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.ReadMemory))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)indirectMemory.ZeroPage);

                // If this is pre-indexed, then add the X register to the zero page for the address lookup
                if (!indirectMemory.IsPostIndexed)
                {
                    LoadRegisterToStack(Ir6502.RegisterName.XIndex, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                    context.IlGenerator.Emit(OpCodes.Conv_U1); // remain in zero-page address
                }

                context.IlGenerator.Emit(OpCodes.Dup); // Since we need two reads
                SaveStackToTempLocal(context, 1); // Save one value for low byte read
                context.IlGenerator.Emit(OpCodes.Ldc_I4, 1);
                context.IlGenerator.Emit(OpCodes.Add); // For the high byte memory address

                // Retrieve the address high byte from memory,
                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, 8);
                context.IlGenerator.Emit(OpCodes.Shl);
                SaveStackToTempLocal(context, 0);

                // This should leave us with the low byte address (pre-dup)
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context, 1);
                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                LoadTempLocalToStack(context, 0);
                context.IlGenerator.Emit(OpCodes.Add); // Add them together for a full 16-bit address

                // Since we need to do a memory lookup, we need to save the current address we read
                // to a temp variable, so we can load the hardware field on the stack before the
                // address for a proper read.
                SaveStackToTempLocal(context);
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);

                if (indirectMemory.IsPostIndexed)
                {
                    // If this is post-indexed, then add the Y register to the result to get the final address
                    LoadRegisterToStack(Ir6502.RegisterName.YIndex, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                }

                // Retrieve the value from the address we now have
                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;
            }

            case Ir6502.Register register:
                LoadRegisterToStack(register.Name, context);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;

            case Ir6502.StackPointer:
                var getStackPointerMethod = typeof(I6502Hal)
                    .GetProperty(nameof(I6502Hal.StackPointer))!
                    .GetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Callvirt, getStackPointerMethod);
                break;

            case Ir6502.Variable variable:
                context.IlGenerator.Emit(OpCodes.Ldloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

    private static void WriteTempLocalToValue(Ir6502.Value destination, Context context)
    {
        switch (destination)
        {
            case Ir6502.AllFlags:
                var setStatusMethod = typeof(I6502Hal)
                    .GetProperty(nameof(I6502Hal.ProcessorStatus))!
                    .SetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setStatusMethod);
                break;

            case Ir6502.Constant constant:
                throw new InvalidOperationException("Can't write to constant");

            case Ir6502.Flag flag:
                var setFlagMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.SetFlag))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                LoadTempLocalToStack(context);
                // Convert int to bool (0 = false, anything else = true)
                context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                context.IlGenerator.Emit(OpCodes.Cgt_Un);
                context.IlGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
                break;

            case Ir6502.Memory memory:
            {
                var writeMemoryMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.WriteMemory))!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                    if (memory.SingleByteAddress)
                    {
                        context.IlGenerator.Emit(OpCodes.Conv_U1);
                    }
                }

                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
                break;
            }

            case Ir6502.IndirectMemory indirectMemory:
            {
                // WARNING: Since the value we want to write ultimately is in temp index 0
                // no code in here should save to that index.
                var readMemoryMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.ReadMemory))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)indirectMemory.ZeroPage);

                // If this is pre-indexed, then add the X register to the zero page for the address lookup
                if (!indirectMemory.IsPostIndexed)
                {
                    LoadRegisterToStack(Ir6502.RegisterName.XIndex, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                    context.IlGenerator.Emit(OpCodes.Conv_U1); // remain in zero-page address
                }

                context.IlGenerator.Emit(OpCodes.Dup); // Since we need two reads
                SaveStackToTempLocal(context, 2); // Save one value for low byte read
                context.IlGenerator.Emit(OpCodes.Ldc_I4, 1);
                context.IlGenerator.Emit(OpCodes.Add); // For the high byte memory address

                // Retrieve the address high byte from memory,
                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, 8);
                context.IlGenerator.Emit(OpCodes.Shl);
                SaveStackToTempLocal(context, 1);

                // This should leave us with the low byte address (pre-dup)
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context, 2);
                context.IlGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                LoadTempLocalToStack(context, 1);
                context.IlGenerator.Emit(OpCodes.Add); // Add them together for a full 16-bit address

                // Since we need to do a memory lookup, we need to save the current address we read
                // to a temp variable, so we can load the hardware field on the stack before the
                // address for a proper read.
                SaveStackToTempLocal(context, 1);
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context, 1);

                if (indirectMemory.IsPostIndexed)
                {
                    // If this is post-indexed, then add the Y register to the result to get the final address
                    LoadRegisterToStack(Ir6502.RegisterName.YIndex, context);
                    context.IlGenerator.Emit(OpCodes.Add);
                }

                // Put the value we want to write on the stack
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte

                // Write the value to the address we now have
                var writeMemoryMethod = typeof(I6502Hal).GetMethod(nameof(I6502Hal.WriteMemory))!;
                context.IlGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
                break;
            }

            case Ir6502.Register register:
                var setMethod = register.Name switch
                {
                    Ir6502.RegisterName.Accumulator => typeof(I6502Hal)
                        .GetProperty(nameof(I6502Hal.ARegister))!
                        .SetMethod!,

                    Ir6502.RegisterName.XIndex => typeof(I6502Hal)
                        .GetProperty(nameof(I6502Hal.XRegister))!
                        .SetMethod!,

                    Ir6502.RegisterName.YIndex => typeof(I6502Hal)
                        .GetProperty(nameof(I6502Hal.YRegister))!
                        .SetMethod!,

                    _ => throw new NotSupportedException(register.Name.ToString()),
                };

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setMethod);

                break;

            case Ir6502.StackPointer:
                var setStackPointerMethod = typeof(I6502Hal)
                    .GetProperty(nameof(I6502Hal.StackPointer))!
                    .SetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setStackPointerMethod);
                break;

            case Ir6502.Variable variable:
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Stloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(destination.GetType().FullName);
        }
    }

    private static void LoadTempLocalToStack(Context context, int index = 0)
    {
        context.IlGenerator.Emit(OpCodes.Ldloc, index);
    }

    private static void SaveStackToTempLocal(Context context, int index = 0)
    {
        context.IlGenerator.Emit(OpCodes.Stloc, index);
    }

    private static void LoadRegisterToStack(Ir6502.RegisterName registerName, Context context)
    {
        var getMethod = registerName switch
        {
            Ir6502.RegisterName.Accumulator => typeof(I6502Hal)
                .GetProperty(nameof(I6502Hal.ARegister))!
                .GetMethod!,

            Ir6502.RegisterName.XIndex => typeof(I6502Hal)
                .GetProperty(nameof(I6502Hal.XRegister))!
                .GetMethod!,

            Ir6502.RegisterName.YIndex => typeof(I6502Hal)
                .GetProperty(nameof(I6502Hal.YRegister))!
                .GetMethod!,

            _ => throw new NotSupportedException(registerName.ToString()),
        };

        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, getMethod);
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