using System.Reflection.Emit;
using DotNesJit.Common.Hal;

namespace DotNesJit.Common.Compilation;

/// <summary>
/// Generates MSIL for a NES Intermediary Representation instruction
/// </summary>
public static class MsilGenerator
{
    public static void Generate(NesIr.Instruction instruction, ILGenerator ilGenerator, GameClass gameClass)
    {
        switch (instruction)
        {
            case NesIr.Copy copy:
                GenerateCopy(copy, ilGenerator, gameClass);
                break;

            case NesIr.Return:
                GenerateRet(ilGenerator);
                break;

            default:
                throw new NotSupportedException(instruction.GetType().FullName);
        }
    }

    private static void GenerateCopy(NesIr.Copy copy, ILGenerator ilGenerator, GameClass gameClass)
    {
        // Read from source
        LoadValueToStack(copy.Source, ilGenerator, gameClass);
    }

    private static void GenerateRet(ILGenerator ilGenerator)
    {
        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void LoadValueToStack(NesIr.Value value, ILGenerator ilGenerator, GameClass gameClass)
    {
        switch (value)
        {
            case NesIr.AllFlags:
                var getStatusMethod = typeof(INesHal)
                    .GetProperty(nameof(INesHal.ProcessorStatus))!
                    .SetMethod!;

                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.HardwareField);
                ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
                break;

            case NesIr.Constant constant:
                ilGenerator.Emit(OpCodes.Ldc_I4, constant.Number);
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

            case NesIr.Variable variable:
                ilGenerator.Emit(OpCodes.Ldloc, variable.Index);
                break;

            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }

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