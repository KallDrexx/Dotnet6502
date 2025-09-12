using System.ComponentModel;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.Syntax;

/// <summary>
/// Converts a disassembled 6502 instruction into an AST instruction
/// </summary>
public class InstructionConverter
{
    private int _variableCount;

    public NesAst.Instruction[] Convert(DisassembledInstruction instruction)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "ADC":
                return ConvertAdc(instruction);

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private NesAst.Instruction[] ConvertAdc(DisassembledInstruction instruction)
    {
        var accumulator = new NesAst.Register(NesAst.RegisterName.Accumulator);
        var operand = ParseOperand(instruction.Info.AddressingMode, instruction.Operands);
        var addVariable = NextVariable();
        var carryVariable = NextVariable();
        var isNewCarryVariable = NextVariable();
        var isZero = NextVariable();
        var isNegative = NextVariable();

        var getCarry = new NesAst.GetFlag(NesAst.Flag.Carry, carryVariable);
        var firstAdd = new NesAst.Binary(NesAst.BinaryOperator.Add, accumulator, operand, addVariable);
        var carryAdd = new NesAst.Binary(NesAst.BinaryOperator.Add, addVariable, carryVariable, accumulator);
        var checkForOverflow = new NesAst.Binary(
            NesAst.BinaryOperator.GreaterThan,
            addVariable,
            new NesAst.Constant(255),
            isNewCarryVariable);

        var checkForZero = new NesAst.Binary(
            NesAst.BinaryOperator.Equals,
            addVariable,
            new NesAst.Constant(0),
            isZero);

        var checkForNegative = new NesAst.Binary(
            NesAst.BinaryOperator.And,
            addVariable,
            new NesAst.Constant(0x80),
            isNegative);

        var setOverflow = new NesAst.SetFlag(NesAst.Flag.Overflow, isNewCarryVariable);
        var setNegative = new NesAst.SetFlag(NesAst.Flag.Negative, isNegative);
        var setZero = new NesAst.SetFlag(NesAst.Flag.Zero, isZero);

        var storeAccumulator = new NesAst.Copy(addVariable, accumulator);

        return
        [
            getCarry, firstAdd, carryAdd, checkForOverflow, checkForZero, checkForNegative, setOverflow, setZero,
            setNegative, storeAccumulator
        ];
    }

    private NesAst.Value ParseOperand(AddressingMode addressingMode, byte[] operands)
    {
        switch (addressingMode)
        {
            case AddressingMode.Immediate:
                return new NesAst.Constant(operands[0]);

            case AddressingMode.ZeroPage:
                return new NesAst.Memory(operands[0], null);

            case AddressingMode.ZeroPageX:
                return new NesAst.Memory(operands[0], NesAst.RegisterName.XIndex);

            case AddressingMode.ZeroPageY:
                return new NesAst.Memory(operands[0], NesAst.RegisterName.YIndex);

            case AddressingMode.Absolute:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesAst.Memory(fullAddress, null);
            }

            case AddressingMode.AbsoluteX:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesAst.Memory(fullAddress, NesAst.RegisterName.XIndex);
            }

            case AddressingMode.AbsoluteY:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesAst.Memory(fullAddress, NesAst.RegisterName.YIndex);
            }

            default:
                throw new NotSupportedException(addressingMode.ToString());
        }
    }

    private NesAst.Variable NextVariable()
    {
        return new NesAst.Variable($"var_{++_variableCount}");
    }
}