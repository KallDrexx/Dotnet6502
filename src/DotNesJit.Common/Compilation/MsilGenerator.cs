using System.Reflection.Emit;
using DotNesJit.Common.Hal;

namespace DotNesJit.Common.Compilation;

/// <summary>
/// Generates MSIL for a NES Intermediary Representation instruction
/// </summary>
public class MsilGenerator
{
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

    public void Generate(NesIr.Instruction instruction, ILGenerator ilGenerator, GameClass gameClass)
    {
        switch (instruction)
        {
            case NesIr.Binary binary:
                GenerateBinary(binary, ilGenerator, gameClass);
                break;

            case NesIr.CallFunction callFunction:
                GenerateCallFunction(callFunction, ilGenerator, gameClass);
                break;

            case NesIr.Copy copy:
                GenerateCopy(copy, ilGenerator, gameClass);
                break;

            case NesIr.InvokeSoftwareInterrupt:
                GenerateInvokeIrq(ilGenerator, gameClass);
                break;

            case NesIr.Jump jump:
                GenerateJump(jump, ilGenerator);
                break;

            case NesIr.JumpIfNotZero jump:
                GenerateJumpIfNotZero(jump, ilGenerator, gameClass);
                break;

            case NesIr.JumpIfZero jump:
                GenerateJumpIfZero(jump, ilGenerator, gameClass);
                break;

            case NesIr.Label label:
                GenerateLabel(label, ilGenerator);
                break;

            case NesIr.PopStackValue pop:
                GeneratePopStackValue(pop, ilGenerator, gameClass);
                break;

            case NesIr.PushStackValue push:
                GeneratePushStackValue(push, ilGenerator, gameClass);
                break;

            case NesIr.Return:
                GenerateReturn(ilGenerator);
                break;

            case NesIr.Unary unary:
                GenerateUnary(unary, ilGenerator, gameClass);
                break;

            case NesIr.WrapValueToByte wrap:
                GenerateWrapToByte(wrap, ilGenerator, gameClass);
                break;

            default:
                throw new NotSupportedException(instruction.GetType().FullName);
        }
    }

    private static void GenerateBinary(NesIr.Binary binary, ILGenerator ilGenerator, GameClass gameClass)
    {
        LoadValueToStack(binary.Left, ilGenerator, gameClass);
        LoadValueToStack(binary.Right, ilGenerator, gameClass);
        EmitBinaryOperator();
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(binary.Destination, ilGenerator, gameClass);
        return;

        void EmitBinaryOperator()
        {
            switch (binary.Operator)
            {
                case NesIr.BinaryOperator.Add:
                    ilGenerator.Emit(OpCodes.Add);
                    break;

                case NesIr.BinaryOperator.And:
                    ilGenerator.Emit(OpCodes.And);
                    break;

                case NesIr.BinaryOperator.Equals:
                    ilGenerator.Emit(OpCodes.Ceq);
                    break;

                case NesIr.BinaryOperator.GreaterThan:
                    ilGenerator.Emit(OpCodes.Cgt);
                    break;

                case NesIr.BinaryOperator.GreaterThanOrEqualTo:
                    ilGenerator.Emit(OpCodes.Clt);
                    ilGenerator.Emit(OpCodes.Neg);
                    break;

                case NesIr.BinaryOperator.LessThan:
                    ilGenerator.Emit(OpCodes.Clt);
                    break;

                case NesIr.BinaryOperator.LessThanOrEqualTo:
                    ilGenerator.Emit(OpCodes.Cgt);
                    ilGenerator.Emit(OpCodes.Neg);
                    break;

                case NesIr.BinaryOperator.NotEquals:
                    ilGenerator.Emit(OpCodes.Ceq);
                    ilGenerator.Emit(OpCodes.Neg);
                    break;

                case NesIr.BinaryOperator.Or:
                    ilGenerator.Emit(OpCodes.Or);
                    break;

                case NesIr.BinaryOperator.ShiftLeft:
                    ilGenerator.Emit(OpCodes.Shl);
                    break;
            
                case NesIr.BinaryOperator.ShiftRight:
                    ilGenerator.Emit(OpCodes.Shr);
                    break;
            
                case NesIr.BinaryOperator.Subtract:
                    ilGenerator.Emit(OpCodes.Sub);
                    break;
            
                case NesIr.BinaryOperator.Xor:
                    ilGenerator.Emit(OpCodes.Xor);
                    break;
            
                default:
                    throw new NotSupportedException(binary.Operator.ToString());
            }
        }
    }

    private static void GenerateCallFunction(NesIr.CallFunction callFunction, ILGenerator ilGenerator, GameClass gameClass)
    {
        if (!gameClass.NesMethods.TryGetValue(callFunction.Name.Characters, out var methodInfo))
        {
            var message = $"No known method with the name '{callFunction.Name.Characters}' exists";
            throw new InvalidOperationException(message);
        }

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        ilGenerator.Emit(OpCodes.Callvirt, methodInfo);
    }

    private static void GenerateCopy(NesIr.Copy copy, ILGenerator ilGenerator, GameClass gameClass)
    {
        LoadValueToStack(copy.Source, ilGenerator, gameClass);
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(copy.Destination, ilGenerator, gameClass);
    }

    private static void GenerateInvokeIrq(ILGenerator ilGenerator, GameClass gameClass)
    {
        var invokeMethod = typeof(INesHal).GetMethod(nameof(INesHal.TriggerSoftwareInterrupt))!;
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        ilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
    }

    private void GenerateJump(NesIr.Jump jump, ILGenerator ilGenerator)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        ilGenerator.Emit(OpCodes.Br, label);
    }

    private void GenerateJumpIfZero(NesIr.JumpIfZero jump, ILGenerator ilGenerator, GameClass gameClass)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, ilGenerator, gameClass);
        ilGenerator.Emit(OpCodes.Brfalse, label);
    }

    private void GenerateJumpIfNotZero(NesIr.JumpIfNotZero jump, ILGenerator ilGenerator, GameClass gameClass)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if not zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, ilGenerator, gameClass);
        ilGenerator.Emit(OpCodes.Brtrue, label);
    }

    private void GenerateLabel(NesIr.Label label, ILGenerator ilGenerator)
    {
        // This does not define the label, but instead marks the label at the current spot. This
        // means the label must already have been defined.
        if (!_labels.TryGetValue(label.Name, out var ilLabel))
        {
            var message = $"Attempted to mark label position '{label.Name.Characters}' but that label has not" +
                          $"been defined";
            throw new InvalidOperationException(message);
        }

        ilGenerator.MarkLabel(ilLabel);
    }

    private static void GeneratePopStackValue(NesIr.PopStackValue pop, ILGenerator ilGenerator, GameClass gameClass)
    {
        var popMethod = typeof(INesHal).GetMethod(nameof(INesHal.PopFromStack))!;
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        ilGenerator.Emit(OpCodes.Callvirt, popMethod);
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(pop.Destination, ilGenerator, gameClass);
    }

    private static void GeneratePushStackValue(NesIr.PushStackValue push, ILGenerator ilGenerator, GameClass gameClass)
    {
        var pushMethod = typeof(INesHal).GetMethod(nameof(INesHal.PushToStack))!;
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        LoadValueToStack(push.Source, ilGenerator, gameClass);
        ilGenerator.Emit(OpCodes.Callvirt, pushMethod);
    }

    private static void GenerateReturn(ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void GenerateUnary(NesIr.Unary unary, ILGenerator ilGenerator, GameClass gameClass)
    {
        LoadValueToStack(unary.Source, ilGenerator, gameClass);

        switch (unary.Operator)
        {
            case NesIr.UnaryOperator.BitwiseNot:
                ilGenerator.Emit(OpCodes.Not);
                break;
            
            default:
                throw new NotSupportedException(unary.Operator.ToString());
        }
        
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(unary.Destination, ilGenerator, gameClass);
    }

    private static void GenerateWrapToByte(NesIr.WrapValueToByte wrap, ILGenerator ilGenerator, GameClass gameClass)
    {
        throw new NotImplementedException();
    }
    
    private static void LoadValueToStack(NesIr.Value value, ILGenerator ilGenerator, GameClass gameClass)
    {
        switch (value)
        {
            case NesIr.AllFlags:
                var getStatusMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.ProcessorStatus))!
                    .GetMethod!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
                break;

            case NesIr.Constant constant:
                ilGenerator.Emit(OpCodes.Ldc_I4, constant.Number);
                break;

            case NesIr.Flag flag:
                var getFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.GetFlag))!;
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag.FlagName);
                ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);
                break;

            case NesIr.Memory memory:
                var readMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.ReadMemory))!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, ilGenerator, gameClass);
                    ilGenerator.Emit(OpCodes.Add);
                }

                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                break;

            case NesIr.Register register:
                LoadRegisterToStack(register.Name, ilGenerator, gameClass);
                break;

            case NesIr.StackPointer:
                var getStackPointerMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.StackPointer))!
                    .GetMethod!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Callvirt, getStackPointerMethod);
                break;

            case NesIr.Variable variable:
                ilGenerator.Emit(OpCodes.Ldloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

    private static void WriteTempLocalToValue(NesIr.Value destination, ILGenerator ilGenerator, GameClass gameClass)
    {
        switch (destination)
        {
            case NesIr.AllFlags:
                var setStatusMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.ProcessorStatus))!
                    .SetMethod!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, setStatusMethod);
                break;

            case NesIr.Constant constant:
                throw new InvalidOperationException("Can't write to constant");

            case NesIr.Flag flag:
                var setFlagMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetFlag))!;
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)flag.FlagName);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
                break;

            case NesIr.Memory memory:
                var writeMemoryMethod = typeof(INesHal).GetMethod(nameof(INesHal.WriteMemory))!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, ilGenerator, gameClass);
                    ilGenerator.Emit(OpCodes.Add);
                }

                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
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

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, setMethod);

                break;

            case NesIr.StackPointer:
                var setStackPointerMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.StackPointer))!
                    .SetMethod!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Callvirt, setStackPointerMethod);
                break;

            case NesIr.Variable variable:
                ilGenerator.Emit(OpCodes.Stloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(destination.GetType().FullName);
        }
    }

    private static void LoadTempLocalToStack(ILGenerator ilGenerator) => ilGenerator.Emit(OpCodes.Ldloc_0);
    private static void SaveStackToTempLocal(ILGenerator ilGenerator) => ilGenerator.Emit(OpCodes.Stloc_0);

    private static void LoadRegisterToStack(
        NesIr.RegisterName registerName,
        ILGenerator ilGenerator,
        GameClass gameClass)
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

        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
        ilGenerator.Emit(OpCodes.Callvirt, getMethod);
    }
}