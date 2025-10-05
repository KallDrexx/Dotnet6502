using System.Reflection.Emit;

namespace Dotnet6502.Common;

/// <summary>
/// Generates MSIL for a NES Intermediary Representation instruction
/// </summary>
public class MsilGenerator
{
    public delegate void CustomIlGenerator(Ir6502.Instruction instruction, ILGenerator ilGenerator);

    private readonly IReadOnlyDictionary<Type, CustomIlGenerator> _customIlGenerators;

    /// <summary>
    /// The number of locals that are required for MsilGenerator operations. It is assumed that
    /// the method the ILGenerator is tied to already has this number of locals PLUS the locals
    /// required by the instructions used in the method. This means that any instruction's local
    /// index must be incremented by this amount.
    /// </summary>
    private const int TemporaryLocalsRequired = 4;

    /// <summary>
    /// Index of the local that will store the debug string
    /// </summary>
    private const int InstructionDebugStringLocalIndex = 3;

    private readonly IReadOnlyDictionary<Ir6502.Identifier, Label> _labels;

    public MsilGenerator(
        IReadOnlyDictionary<Ir6502.Identifier, Label> labels,
        IReadOnlyDictionary<Type, CustomIlGenerator>? customIlGenerators)
    {
        _labels = labels;
        _customIlGenerators = customIlGenerators ?? new Dictionary<Type, CustomIlGenerator>();
    }

    public static void DeclareRequiredLocals(ILGenerator ilGenerator)
    {
        // All locals except one are for ints, with the string local used for debugging assistance
        for (var x = 0; x < TemporaryLocalsRequired - 1; x++)
        {
            ilGenerator.DeclareLocal(typeof(int));
        }

        ilGenerator.DeclareLocal(typeof(string));
    }

    public void Generate(Ir6502.Instruction instruction, ILGenerator ilGenerator)
    {
        if (_customIlGenerators.TryGetValue(instruction.GetType(), out var generator))
        {
            generator.Invoke(instruction, ilGenerator);
            return;
        }
        
        switch (instruction)
        {
            case Ir6502.Binary binary:
                GenerateBinary(binary, ilGenerator);
                break;

            case Ir6502.CallFunction callFunction:
                GenerateCallFunction(callFunction, ilGenerator);
                break;

            case Ir6502.ConvertVariableToByte convertVariableToByte:
                GenerateConvertToByte(convertVariableToByte, ilGenerator);
                break;

            case Ir6502.Copy copy:
                GenerateCopy(copy, ilGenerator);
                break;

            case Ir6502.InvokeSoftwareInterrupt:
                GenerateInvokeIrq(ilGenerator);
                break;

            case Ir6502.Jump jump:
                GenerateJump(jump, ilGenerator);
                break;

            case Ir6502.JumpIfNotZero jump:
                GenerateJumpIfNotZero(jump, ilGenerator);
                break;

            case Ir6502.JumpIfZero jump:
                GenerateJumpIfZero(jump, ilGenerator);
                break;

            case Ir6502.Label label:
                GenerateLabel(label, ilGenerator);
                break;

            case Ir6502.PopStackValue pop:
                GeneratePopStackValue(pop, ilGenerator);
                break;

            case Ir6502.PushStackValue push:
                GeneratePushStackValue(push, ilGenerator);
                break;

            case Ir6502.Return:
                GenerateReturn(ilGenerator);
                break;

            case Ir6502.Unary unary:
                GenerateUnary(unary, ilGenerator);
                break;

            case Ir6502.StoreDebugString debugString:
                GenerateDebugString(debugString, ilGenerator);
                break;

            default:
                throw new NotSupportedException(instruction.GetType().FullName);
        }
    }

    private static void GenerateBinary(Ir6502.Binary binary, ILGenerator ilGenerator)
    {
        LoadValueToStack(binary.Left, ilGenerator);
        LoadValueToStack(binary.Right, ilGenerator);
        EmitBinaryOperator();
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(binary.Destination, ilGenerator);
        return;

        void EmitBinaryOperator()
        {
            switch (binary.Operator)
            {
                case Ir6502.BinaryOperator.Add:
                    ilGenerator.Emit(OpCodes.Add);
                    break;

                case Ir6502.BinaryOperator.And:
                    ilGenerator.Emit(OpCodes.And);
                    break;

                case Ir6502.BinaryOperator.Equals:
                    ilGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.GreaterThan:
                    ilGenerator.Emit(OpCodes.Cgt);
                    break;

                case Ir6502.BinaryOperator.GreaterThanOrEqualTo:
                    ilGenerator.Emit(OpCodes.Clt);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.LessThan:
                    ilGenerator.Emit(OpCodes.Clt);
                    break;

                case Ir6502.BinaryOperator.LessThanOrEqualTo:
                    ilGenerator.Emit(OpCodes.Cgt);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.NotEquals:
                    ilGenerator.Emit(OpCodes.Ceq);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Ceq);
                    break;

                case Ir6502.BinaryOperator.Or:
                    ilGenerator.Emit(OpCodes.Or);
                    break;

                case Ir6502.BinaryOperator.ShiftLeft:
                    ilGenerator.Emit(OpCodes.Shl);
                    break;
            
                case Ir6502.BinaryOperator.ShiftRight:
                    ilGenerator.Emit(OpCodes.Shr);
                    break;
            
                case Ir6502.BinaryOperator.Subtract:
                    ilGenerator.Emit(OpCodes.Sub);
                    break;
            
                case Ir6502.BinaryOperator.Xor:
                    ilGenerator.Emit(OpCodes.Xor);
                    break;
            
                default:
                    throw new NotSupportedException(binary.Operator.ToString());
            }
        }
    }

    private static void GenerateCallFunction(Ir6502.CallFunction callFunction, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(JitCompiler.LoadJitCompilerArg);

        if (callFunction.Address.IsIndirect)
        {
            // Look up the pointer to find the location of the function to call
            var readMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.ReadMemory))!;
            ilGenerator.Emit(JitCompiler.LoadHalArg);
            ilGenerator.Emit(OpCodes.Ldc_I4, callFunction.Address.Address);
            ilGenerator.Emit(OpCodes.Dup);
            SaveStackToTempLocal(ilGenerator, 0); // save for LSB read

            // Read the MSB
            ilGenerator.Emit(OpCodes.Ldc_I4, 1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
            ilGenerator.Emit(OpCodes.Conv_I4);
            ilGenerator.Emit(OpCodes.Ldc_I4, 8);
            ilGenerator.Emit(OpCodes.Shl);
            SaveStackToTempLocal(ilGenerator, 1);

            // Read the LSB
            ilGenerator.Emit(JitCompiler.LoadHalArg);
            LoadTempLocalToStack(ilGenerator, 0);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
            ilGenerator.Emit(OpCodes.Conv_I4);

            // Combine them together for the full address
            LoadTempLocalToStack(ilGenerator, 1);
            ilGenerator.Emit(OpCodes.Or);
        }
        else
        {
            // Direct call to the provided address
            ilGenerator.Emit(OpCodes.Ldc_I4, callFunction.Address.Address);
        }
        
        var method = typeof(IJitCompiler).GetMethod(nameof(JitCompiler.RunMethod))!;
        ilGenerator.Emit(OpCodes.Callvirt, method);
    }

    private static void GenerateCopy(Ir6502.Copy copy, ILGenerator ilGenerator)
    {
        LoadValueToStack(copy.Source, ilGenerator);
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(copy.Destination, ilGenerator);
    }

    private static void GenerateInvokeIrq(ILGenerator ilGenerator)
    {
        var invokeMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.TriggerSoftwareInterrupt))!;
        ilGenerator.Emit(JitCompiler.LoadHalArg);
        ilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
    }

    private void GenerateJump(Ir6502.Jump jump, ILGenerator ilGenerator)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        ilGenerator.Emit(OpCodes.Br, label);
    }

    private void GenerateJumpIfZero(Ir6502.JumpIfZero jump, ILGenerator ilGenerator)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, ilGenerator);
        ilGenerator.Emit(OpCodes.Brfalse, label);
    }

    private void GenerateJumpIfNotZero(Ir6502.JumpIfNotZero jump, ILGenerator ilGenerator)
    {
        if (!_labels.TryGetValue(jump.Target, out var label))
        {
            var message = $"Jump if not zero received to target '{jump.Target}' but no label exists for that.";
            throw new InvalidOperationException(message);
        }

        LoadValueToStack(jump.Condition, ilGenerator);
        ilGenerator.Emit(OpCodes.Brtrue, label);
    }

    private void GenerateLabel(Ir6502.Label label, ILGenerator ilGenerator)
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

    private static void GeneratePopStackValue(Ir6502.PopStackValue pop, ILGenerator ilGenerator)
    {
        var popMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.PopFromStack))!;
        ilGenerator.Emit(JitCompiler.LoadHalArg);
        ilGenerator.Emit(OpCodes.Callvirt, popMethod);
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(pop.Destination, ilGenerator);
    }

    private static void GeneratePushStackValue(Ir6502.PushStackValue push, ILGenerator ilGenerator)
    {
        var pushMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.PushToStack))!;
        ilGenerator.Emit(JitCompiler.LoadHalArg);
        LoadValueToStack(push.Source, ilGenerator);
        ilGenerator.Emit(OpCodes.Callvirt, pushMethod);
    }

    private static void GenerateReturn(ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void GenerateUnary(Ir6502.Unary unary, ILGenerator ilGenerator)
    {
        LoadValueToStack(unary.Source, ilGenerator);

        switch (unary.Operator)
        {
            case Ir6502.UnaryOperator.BitwiseNot:
                ilGenerator.Emit(OpCodes.Not);
                break;
            
            default:
                throw new NotSupportedException(unary.Operator.ToString());
        }
        
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(unary.Destination, ilGenerator);
    }

    private static void GenerateConvertToByte(Ir6502.ConvertVariableToByte convertVariableToByte, ILGenerator ilGenerator)
    {
        LoadValueToStack(convertVariableToByte.Variable, ilGenerator);
        ilGenerator.Emit(OpCodes.Conv_U1);
        SaveStackToTempLocal(ilGenerator);
        WriteTempLocalToValue(convertVariableToByte.Variable, ilGenerator);
    }

    private void GenerateDebugString(Ir6502.StoreDebugString debugString, ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ldstr, debugString.Text);
        ilGenerator.Emit(OpCodes.Stloc, InstructionDebugStringLocalIndex);
    }

    private static void LoadValueToStack(Ir6502.Value value, ILGenerator ilGenerator)
    {
        switch (value)
        {
            case Ir6502.AllFlags:
                var getStatusMethod = typeof(Base6502Hal)
                    .GetProperty(nameof(Base6502Hal.ProcessorStatus))!
                    .GetMethod!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
                break;

            case Ir6502.Constant constant:
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)constant.Number);
                break;

            case Ir6502.Flag flag:
                var getFlagMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.GetFlag))!;
                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);
                ilGenerator.Emit(OpCodes.Conv_I4);
                break;

            case Ir6502.Memory memory:
            {
                var readMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.ReadMemory))!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, ilGenerator);
                    ilGenerator.Emit(OpCodes.Add);
                    if (memory.SingleByteAddress)
                    {
                        ilGenerator.Emit(OpCodes.Conv_U1);
                    }
                }

                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                ilGenerator.Emit(OpCodes.Conv_I4);
                break;
            }

            case Ir6502.IndirectMemory indirectMemory:
                LoadIndirectMemoryValueToStack(indirectMemory, ilGenerator);
                break;

            case Ir6502.Register register:
                LoadRegisterToStack(register.Name, ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_I4);
                break;

            case Ir6502.StackPointer:
                var getStackPointerMethod = typeof(Base6502Hal)
                    .GetProperty(nameof(Base6502Hal.StackPointer))!
                    .GetMethod!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Callvirt, getStackPointerMethod);
                break;

            case Ir6502.Variable variable:
                ilGenerator.Emit(OpCodes.Ldloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

    private static void LoadIndirectMemoryValueToStack(Ir6502.IndirectMemory indirectMemory, ILGenerator ilGenerator)
    {
        var readMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.ReadMemory))!;

        ilGenerator.Emit(JitCompiler.LoadHalArg);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)indirectMemory.ZeroPageAddress);

        // If this is pre-indexed, then add the X register to the zero page for the address lookup
        if (indirectMemory.IsPreIndexed)
        {
            LoadRegisterToStack(Ir6502.RegisterName.XIndex, ilGenerator);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Conv_U1); // remain in zero-page address
        }

        ilGenerator.Emit(OpCodes.Dup); // Since we need two reads
        SaveStackToTempLocal(ilGenerator, 1); // Save one value for low byte read
        ilGenerator.Emit(OpCodes.Ldc_I4, 1);
        ilGenerator.Emit(OpCodes.Add); // For the high byte memory address
        ilGenerator.Emit(OpCodes.Conv_U1); // remain in zero-page address

        // Retrieve the address high byte from memory,
        ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
        ilGenerator.Emit(OpCodes.Conv_I4);
        ilGenerator.Emit(OpCodes.Ldc_I4, 8);
        ilGenerator.Emit(OpCodes.Shl);
        SaveStackToTempLocal(ilGenerator, 0);

        // This should leave us with the low byte address (pre-dup)
        ilGenerator.Emit(JitCompiler.LoadHalArg);
        LoadTempLocalToStack(ilGenerator, 1);
        ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
        ilGenerator.Emit(OpCodes.Conv_I4);
        LoadTempLocalToStack(ilGenerator, 0);
        ilGenerator.Emit(OpCodes.Or); // Combine them together for a full 16-bit address

        // Since we need to do a memory lookup, we need to save the current address we read
        // to a temp variable, so we can load the hardware field on the stack before the
        // address for a proper read.
        SaveStackToTempLocal(ilGenerator);
        ilGenerator.Emit(JitCompiler.LoadHalArg);
        LoadTempLocalToStack(ilGenerator);

        if (indirectMemory.IsPostIndexed)
        {
            // If this is post-indexed, then add the Y register to the result to get the final address
            LoadRegisterToStack(Ir6502.RegisterName.YIndex, ilGenerator);
            ilGenerator.Emit(OpCodes.Add);
        }

        // Retrieve the value from the address we now have
        ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
        ilGenerator.Emit(OpCodes.Conv_I4);
    }

    private static void WriteTempLocalToValue(Ir6502.Value destination, ILGenerator ilGenerator)
    {
        switch (destination)
        {
            case Ir6502.AllFlags:
                var setStatusMethod = typeof(Base6502Hal)
                    .GetProperty(nameof(Base6502Hal.ProcessorStatus))!
                    .SetMethod!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                ilGenerator.Emit(OpCodes.Callvirt, setStatusMethod);
                break;

            case Ir6502.Constant constant:
                throw new InvalidOperationException("Can't write to constant");

            case Ir6502.Flag flag:
                var setFlagMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.SetFlag))!;
                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)ConvertFlagName(flag.FlagName));
                LoadTempLocalToStack(ilGenerator);
                // Convert int to bool (0 = false, anything else = true)
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
                ilGenerator.Emit(OpCodes.Cgt_Un);
                ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);
                break;

            case Ir6502.Memory memory:
            {
                var writeMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.WriteMemory))!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Ldc_I4, memory.Address);

                if (memory.RegisterToAdd != null)
                {
                    LoadRegisterToStack(memory.RegisterToAdd.Value, ilGenerator);
                    ilGenerator.Emit(OpCodes.Add);
                    if (memory.SingleByteAddress)
                    {
                        ilGenerator.Emit(OpCodes.Conv_U1);
                    }
                }

                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
                break;
            }

            case Ir6502.IndirectMemory indirectMemory:
            {
                // WARNING: Since the value we want to write ultimately is in temp index 0
                // no code in here should save to that index.
                var readMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.ReadMemory))!;
                ilGenerator.Emit(JitCompiler.LoadHalArg);
                ilGenerator.Emit(OpCodes.Ldc_I4, (int)indirectMemory.ZeroPageAddress);

                // If this is pre-indexed, then add the X register to the zero page for the address lookup
                if (indirectMemory.IsPreIndexed)
                {
                    LoadRegisterToStack(Ir6502.RegisterName.XIndex, ilGenerator);
                    ilGenerator.Emit(OpCodes.Add);
                    ilGenerator.Emit(OpCodes.Conv_U1); // remain in zero-page address
                }

                ilGenerator.Emit(OpCodes.Dup); // Since we need two reads
                SaveStackToTempLocal(ilGenerator, 2); // Save one value for low byte read
                ilGenerator.Emit(OpCodes.Ldc_I4, 1);
                ilGenerator.Emit(OpCodes.Add); // For the high byte memory address
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert back to zero page

                // Retrieve the address high byte from memory,
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                ilGenerator.Emit(OpCodes.Conv_I4);
                ilGenerator.Emit(OpCodes.Ldc_I4, 8);
                ilGenerator.Emit(OpCodes.Shl);
                SaveStackToTempLocal(ilGenerator, 1);

                // This should leave us with the low byte address (pre-dup)
                ilGenerator.Emit(JitCompiler.LoadHalArg);
                LoadTempLocalToStack(ilGenerator, 2);
                ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                ilGenerator.Emit(OpCodes.Conv_I4);
                LoadTempLocalToStack(ilGenerator, 1);
                ilGenerator.Emit(OpCodes.Or); // Add them together for a full 16-bit address

                // Since we need to do a memory lookup, we need to save the current address we read
                // to a temp variable, so we can load the hardware field on the stack before the
                // address for a proper read.
                SaveStackToTempLocal(ilGenerator, 1);
                ilGenerator.Emit(JitCompiler.LoadHalArg);
                LoadTempLocalToStack(ilGenerator, 1);

                if (indirectMemory.IsPostIndexed)
                {
                    // If this is post-indexed, then add the Y register to the result to get the final address
                    LoadRegisterToStack(Ir6502.RegisterName.YIndex, ilGenerator);
                    ilGenerator.Emit(OpCodes.Add);
                }

                // Put the value we want to write on the stack
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte

                // Write the value to the address we now have
                var writeMemoryMethod = typeof(Base6502Hal).GetMethod(nameof(Base6502Hal.WriteMemory))!;
                ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
                break;
            }

            case Ir6502.Register register:
                var setMethod = register.Name switch
                {
                    Ir6502.RegisterName.Accumulator => typeof(Base6502Hal)
                        .GetProperty(nameof(Base6502Hal.ARegister))!
                        .SetMethod!,

                    Ir6502.RegisterName.XIndex => typeof(Base6502Hal)
                        .GetProperty(nameof(Base6502Hal.XRegister))!
                        .SetMethod!,

                    Ir6502.RegisterName.YIndex => typeof(Base6502Hal)
                        .GetProperty(nameof(Base6502Hal.YRegister))!
                        .SetMethod!,

                    _ => throw new NotSupportedException(register.Name.ToString()),
                };

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                ilGenerator.Emit(OpCodes.Callvirt, setMethod);

                break;

            case Ir6502.StackPointer:
                var setStackPointerMethod = typeof(Base6502Hal)
                    .GetProperty(nameof(Base6502Hal.StackPointer))!
                    .SetMethod!;

                ilGenerator.Emit(JitCompiler.LoadHalArg);
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Conv_U1); // Convert int to byte
                ilGenerator.Emit(OpCodes.Callvirt, setStackPointerMethod);
                break;

            case Ir6502.Variable variable:
                LoadTempLocalToStack(ilGenerator);
                ilGenerator.Emit(OpCodes.Stloc, variable.Index + TemporaryLocalsRequired);
                break;

            default:
                throw new NotSupportedException(destination.GetType().FullName);
        }
    }

    private static void LoadTempLocalToStack(ILGenerator ilGenerator, int index = 0)
    {
        ilGenerator.Emit(OpCodes.Ldloc, index);
    }

    private static void SaveStackToTempLocal(ILGenerator ilGenerator, int index = 0)
    {
        ilGenerator.Emit(OpCodes.Stloc, index);
    }

    private static void LoadRegisterToStack(Ir6502.RegisterName registerName, ILGenerator ilGenerator)
    {
        var getMethod = registerName switch
        {
            Ir6502.RegisterName.Accumulator => typeof(Base6502Hal)
                .GetProperty(nameof(Base6502Hal.ARegister))!
                .GetMethod!,

            Ir6502.RegisterName.XIndex => typeof(Base6502Hal)
                .GetProperty(nameof(Base6502Hal.XRegister))!
                .GetMethod!,

            Ir6502.RegisterName.YIndex => typeof(Base6502Hal)
                .GetProperty(nameof(Base6502Hal.YRegister))!
                .GetMethod!,

            _ => throw new NotSupportedException(registerName.ToString()),
        };

        ilGenerator.Emit(JitCompiler.LoadHalArg);
        ilGenerator.Emit(OpCodes.Callvirt, getMethod);
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