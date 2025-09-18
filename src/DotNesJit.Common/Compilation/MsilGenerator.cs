using System.Reflection;
using System.Reflection.Emit;
using DotNesJit.Common.Hal;

namespace DotNesJit.Common.Compilation;

/// <summary>
/// Generates MSIL for a NES Intermediary Representation instruction
/// </summary>
public class MsilGenerator
{
    public record Context(
        ILGenerator IlGenerator,
        FieldInfo HardwareField,
        Func<string, MethodInfo?> GetMethodInfo);

    /// <summary>
    /// The number of locals that are required for MsilGenerator operations. It is assumed that
    /// the method the ILGenerator is tied to already has this number of locals PLUS the locals
    /// required by the instructions used in the method. This means that any instruction's local
    /// index must be incremented by this amount.
    /// </summary>
    public const int TemporaryLocalsRequired = 1;

    private readonly IReadOnlyDictionary<NesIr.Identifier, Label> _labels;

    public MsilGenerator(IReadOnlyDictionary<NesIr.Identifier, Label> labels)
    {
        _labels = labels;
    }

    public void Generate(NesIr.Instruction instruction, Context context)
    {
        switch (instruction)
        {
            case NesIr.Binary binary:
                GenerateBinary(binary, context);
                break;

            case NesIr.CallFunction callFunction:
                GenerateCallFunction(callFunction, context);
                break;

            case NesIr.ConvertVariableToByte convertVariableToByte:
                GenerateConvertToByte(convertVariableToByte, context);
                break;

            case NesIr.Copy copy:
                GenerateCopy(copy, context);
                break;

            case NesIr.InvokeSoftwareInterrupt:
                GenerateInvokeIrq(context);
                break;

            case NesIr.Jump jump:
                GenerateJump(jump, context);
                break;

            case NesIr.JumpIfNotZero jump:
                GenerateJumpIfNotZero(jump, context);
                break;

            case NesIr.JumpIfZero jump:
                GenerateJumpIfZero(jump, context);
                break;

            case NesIr.Label label:
                GenerateLabel(label, context);
                break;

            case NesIr.PopStackValue pop:
                GeneratePopStackValue(pop, context);
                break;

            case NesIr.PushStackValue push:
                GeneratePushStackValue(push, context);
                break;

            case NesIr.Return:
                GenerateReturn(context);
                break;

            case NesIr.Unary unary:
                GenerateUnary(unary, context);
                break;

            default:
                throw new NotSupportedException(instruction.GetType().FullName);
        }
    }

    private static void GenerateBinary(NesIr.Binary binary, Context context)
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
                case NesIr.BinaryOperator.Add:
                    context.IlGenerator.Emit(OpCodes.Add);
                    break;

                case NesIr.BinaryOperator.And:
                    context.IlGenerator.Emit(OpCodes.And);
                    break;

                case NesIr.BinaryOperator.Equals:
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case NesIr.BinaryOperator.GreaterThan:
                    context.IlGenerator.Emit(OpCodes.Cgt);
                    break;

                case NesIr.BinaryOperator.GreaterThanOrEqualTo:
                    context.IlGenerator.Emit(OpCodes.Clt);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case NesIr.BinaryOperator.LessThan:
                    context.IlGenerator.Emit(OpCodes.Clt);
                    break;

                case NesIr.BinaryOperator.LessThanOrEqualTo:
                    context.IlGenerator.Emit(OpCodes.Cgt);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case NesIr.BinaryOperator.NotEquals:
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                    context.IlGenerator.Emit(OpCodes.Ceq);
                    break;

                case NesIr.BinaryOperator.Or:
                    context.IlGenerator.Emit(OpCodes.Or);
                    break;

                case NesIr.BinaryOperator.ShiftLeft:
                    context.IlGenerator.Emit(OpCodes.Shl);
                    break;
            
                case NesIr.BinaryOperator.ShiftRight:
                    context.IlGenerator.Emit(OpCodes.Shr);
                    break;
            
                case NesIr.BinaryOperator.Subtract:
                    context.IlGenerator.Emit(OpCodes.Sub);
                    break;
            
                case NesIr.BinaryOperator.Xor:
                    context.IlGenerator.Emit(OpCodes.Xor);
                    break;
            
                default:
                    throw new NotSupportedException(binary.Operator.ToString());
            }
        }
    }

    private static void GenerateCallFunction(NesIr.CallFunction callFunction, Context context)
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

    private static void GenerateCopy(NesIr.Copy copy, Context context)
    {
        LoadValueToStack(copy.Source, context);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(copy.Destination, context);
    }

    private static void GenerateInvokeIrq(Context context)
    {
        var invokeMethod = typeof(INesHal).GetMethod(nameof(INesHal.TriggerSoftwareInterrupt))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, invokeMethod);
    }

    private void GenerateJump(NesIr.Jump jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        context.IlGenerator.Emit(OpCodes.Br, label);
    }

    private void GenerateJumpIfZero(NesIr.JumpIfZero jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, context);
        context.IlGenerator.Emit(OpCodes.Brfalse, label);
    }

    private void GenerateJumpIfNotZero(NesIr.JumpIfNotZero jump, Context context)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if not zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, context);
        context.IlGenerator.Emit(OpCodes.Brtrue, label);
    }

    private void GenerateLabel(NesIr.Label label, Context context)
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

    private static void GeneratePopStackValue(NesIr.PopStackValue pop, Context context)
    {
        var popMethod = typeof(INesHal).GetMethod(nameof(INesHal.PopFromStack))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, popMethod);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(pop.Destination, context);
    }

    private static void GeneratePushStackValue(NesIr.PushStackValue push, Context context)
    {
        var pushMethod = typeof(INesHal).GetMethod(nameof(INesHal.PushToStack))!;
        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        LoadValueToStack(push.Source, context);
        context.IlGenerator.Emit(OpCodes.Callvirt, pushMethod);
    }

    private static void GenerateReturn(Context context)
    {
        context.IlGenerator.Emit(OpCodes.Ret);
    }

    private static void GenerateUnary(NesIr.Unary unary, Context context)
    {
        LoadValueToStack(unary.Source, context);

        switch (unary.Operator)
        {
            case NesIr.UnaryOperator.BitwiseNot:
                context.IlGenerator.Emit(OpCodes.Not);
                break;
            
            default:
                throw new NotSupportedException(unary.Operator.ToString());
        }
        
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(unary.Destination, context);
    }

    private static void GenerateConvertToByte(NesIr.ConvertVariableToByte convertVariableToByte, Context context)
    {
        LoadValueToStack(convertVariableToByte.Variable, context);
        context.IlGenerator.Emit(OpCodes.Conv_U1);
        SaveStackToTempLocal(context);
        WriteTempLocalToValue(convertVariableToByte.Variable, context);
    }

    private static void LoadValueToStack(NesIr.Value value, Context context)
    {
        switch (value)
        {
            case NesIr.AllFlags:
                var getStatusMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.ProcessorStatus))!
                    .GetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
                break;

            case NesIr.Constant constant:
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)constant.Number);
                break;

            case NesIr.Flag flag:
                var getFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.GetFlag))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                context.IlGenerator.Emit(OpCodes.Callvirt, getFlagMethod);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;

            case NesIr.Memory memory:
                var readMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReadMemory))!;

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

            case NesIr.Register register:
                LoadRegisterToStack(register.Name, context);
                context.IlGenerator.Emit(OpCodes.Conv_I4);
                break;

            case NesIr.StackPointer:
                var getStackPointerMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.StackPointer))!
                    .GetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Callvirt, getStackPointerMethod);
                break;

            case NesIr.Variable variable:
                context.IlGenerator.Emit(OpCodes.Ldloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

    private static void WriteTempLocalToValue(NesIr.Value destination, Context context)
    {
        switch (destination)
        {
            case NesIr.AllFlags:
                var setStatusMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.ProcessorStatus))!
                    .SetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setStatusMethod);
                break;

            case NesIr.Constant constant:
                throw new InvalidOperationException("Can't write to constant");

            case NesIr.Flag flag:
                var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag))!;
                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                context.IlGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                LoadTempLocalToStack(context);
                // Convert int to bool (0 = false, anything else = true)
                context.IlGenerator.Emit(OpCodes.Ldc_I4_0);
                context.IlGenerator.Emit(OpCodes.Cgt_Un);
                context.IlGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
                break;

            case NesIr.Memory memory:
                var writeMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.WriteMemory))!;

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

            case NesIr.Register register:
                var setMethod = register.Name switch
                {
                    NesIr.RegisterName.Accumulator => typeof(INesHal)
                        .GetProperty(nameof(INesHal.ARegister))!
                        .SetMethod!,

                    NesIr.RegisterName.XIndex => typeof(INesHal)
                        .GetProperty(nameof(INesHal.XRegister))!
                        .SetMethod!,

                    NesIr.RegisterName.YIndex => typeof(INesHal)
                        .GetProperty(nameof(INesHal.YRegister))!
                        .SetMethod!,

                    _ => throw new NotSupportedException(register.Name.ToString()),
                };

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setMethod);

                break;

            case NesIr.StackPointer:
                var setStackPointerMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.StackPointer))!
                    .SetMethod!;

                context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                context.IlGenerator.Emit(OpCodes.Callvirt, setStackPointerMethod);
                break;

            case NesIr.Variable variable:
                LoadTempLocalToStack(context);
                context.IlGenerator.Emit(OpCodes.Stloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(destination.GetType().FullName);
        }
    }

    private static void LoadTempLocalToStack(Context context) => context.IlGenerator.Emit(OpCodes.Ldloc_0);
    private static void SaveStackToTempLocal(Context context) => context.IlGenerator.Emit(OpCodes.Stloc_0);

    private static void LoadRegisterToStack(NesIr.RegisterName registerName, Context context)
    {
        var getMethod = registerName switch
        {
            NesIr.RegisterName.Accumulator => typeof(INesHal)
                .GetProperty(nameof(INesHal.ARegister))!
                .GetMethod!,

            NesIr.RegisterName.XIndex => typeof(INesHal)
                .GetProperty(nameof(INesHal.XRegister))!
                .GetMethod!,

            NesIr.RegisterName.YIndex => typeof(INesHal)
                .GetProperty(nameof(INesHal.YRegister))!
                .GetMethod!,

            _ => throw new NotSupportedException(registerName.ToString()),
        };

        context.IlGenerator.Emit(OpCodes.Ldsfld, context.HardwareField);
        context.IlGenerator.Emit(OpCodes.Callvirt, getMethod);
    }

    private static CpuStatusFlags ConvertFlagName(NesIr.FlagName flagName)
    {
        return flagName switch
        {
            NesIr.FlagName.Carry => CpuStatusFlags.Carry,
            NesIr.FlagName.Zero => CpuStatusFlags.Zero,
            NesIr.FlagName.InterruptDisable => CpuStatusFlags.InterruptDisable,
            NesIr.FlagName.BFlag => CpuStatusFlags.BFlag,
            NesIr.FlagName.Decimal => CpuStatusFlags.Decimal,
            NesIr.FlagName.Overflow => CpuStatusFlags.Overflow,
            NesIr.FlagName.Negative => CpuStatusFlags.Negative,
            _ => throw new NotSupportedException(flagName.ToString())
        };
    }
}