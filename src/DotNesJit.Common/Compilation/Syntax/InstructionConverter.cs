using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.Syntax;

/// <summary>
/// Converts a disassembled 6502 instruction into an AST instruction
/// </summary>
public class InstructionConverter
{
    public NesIr.Instruction[] Convert(DisassembledInstruction instruction)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "ADC": return ConvertAdc(instruction);
            case "AND": return ConvertAnd(instruction);
            case "ASL": return ConvertAsl(instruction);
            case "BIT": return ConvertBit(instruction);
            case "CLC": return ConvertClc(instruction);
            case "CLD": return ConvertCld(instruction);
            case "CLI": return ConvertCli(instruction);
            case "CLV": return ConvertClv(instruction);

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private NesIr.Instruction[] ConvertAdc(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var isNegative = new NesIr.Variable(0);

        // Don't store the value in the accumulator so we don't lose track of if it overflowed due to byte precision
        var addVariable = new NesIr.Variable(1);

        var firstAdd = new NesIr.Binary(NesIr.BinaryOperator.Add, accumulator, operand, addVariable);
        var carryAdd = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            addVariable,
            new NesIr.Flag(NesIr.FlagName.Carry),
            accumulator);

        var adjustForOverflow = new NesIr.AdjustIfOverflowed(addVariable, new NesIr.Flag(NesIr.FlagName.Overflow));
        var checkForZero = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            addVariable,
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var checkForNegative = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            addVariable,
            new NesIr.Constant(0x80),
            isNegative);

        var setNegative = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            isNegative,
            new NesIr.Constant(0x80),
            new NesIr.Flag(NesIr.FlagName.Negative));

        var storeAccumulator = new NesIr.Copy(addVariable, accumulator);

        return
        [
            firstAdd, carryAdd, adjustForOverflow, checkForZero, checkForNegative, setNegative, storeAccumulator
        ];
    }

    private NesIr.Instruction[] ConvertAnd(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var isNegative = new NesIr.Variable(1);

        var andOperation = new NesIr.Binary(NesIr.BinaryOperator.And, accumulator, operand, accumulator);
        var checkForZero = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            accumulator,
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var checkForNegative = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            accumulator,
            new NesIr.Constant(0x80),
            isNegative);

        var setNegative = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            isNegative,
            new NesIr.Constant(0x80),
            new NesIr.Flag(NesIr.FlagName.Negative));

        return [andOperation, checkForZero, checkForNegative, setNegative];
    }

    private NesIr.Instruction[] ConvertAsl(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var tempVariable = new NesIr.Variable(0);

        var carry = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            operand,
            new NesIr.Constant(0x80),
            tempVariable);

        var carryFlag = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            tempVariable,
            new NesIr.Constant(0x80),
            new NesIr.Flag(NesIr.FlagName.Carry));

        var shift = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftLeft,
            operand,
            new NesIr.Constant(1),
            operand);

        var zeroFlag = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            operand,
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var checkForNegative = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            operand,
            new NesIr.Constant(0x80),
            tempVariable);

        var setNegative = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            tempVariable,
            new NesIr.Constant(0x80),
            new NesIr.Flag(NesIr.FlagName.Negative));

        return [carry, carryFlag, shift, zeroFlag, checkForNegative, setNegative];
    }

    private NesIr.Instruction[] ConvertBit(DisassembledInstruction instruction)
    {
        var tempVariable = new NesIr.Variable(0);
        var operand = ParseAddress(instruction);

        var andOp = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            operand,
            tempVariable);

        var zeroFlag = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            tempVariable,
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Zero));

        var overflow = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThan,
            tempVariable,
            new NesIr.Constant(255),
            new NesIr.Flag(NesIr.FlagName.Overflow));

        var negativeFlag = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            tempVariable,
            new NesIr.Constant(0x40),
            tempVariable);

        var setNegative = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            tempVariable,
            new NesIr.Constant(0x40),
            new NesIr.Flag(NesIr.FlagName.Negative));

        return [andOp, zeroFlag, overflow, negativeFlag, setNegative];
    }

    private NesIr.Instruction[] ConvertClc(DisassembledInstruction instruction)
    {
        var setCarry = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Carry), new NesIr.Constant(0));

        return [setCarry];
    }

    private NesIr.Instruction[] ConvertCld(DisassembledInstruction instruction)
    {
        var setDecimal = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Decimal), new NesIr.Constant(0));

        return [setDecimal];
    }

    private NesIr.Instruction[] ConvertCli(DisassembledInstruction instruction)
    {
        var setInterrupt = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.InterruptDisable), new NesIr.Constant(0));

        return [setInterrupt];
    }

    private NesIr.Instruction[] ConvertClv(DisassembledInstruction instruction)
    {
        var setOverflow = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Overflow), new NesIr.Constant(0));

        return [setOverflow];
    }

    private NesIr.Value ParseAddress(DisassembledInstruction instruction)
    {
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.Immediate:
                return new NesIr.Constant(instruction.Operands[0]);

            case AddressingMode.ZeroPage:
                return new NesIr.Memory(instruction.Operands[0], null);

            case AddressingMode.ZeroPageX:
                return new NesIr.Memory(instruction.Operands[0], NesIr.RegisterName.XIndex);

            case AddressingMode.ZeroPageY:
                return new NesIr.Memory(instruction.Operands[0], NesIr.RegisterName.YIndex);

            case AddressingMode.Absolute:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new NesIr.Memory(fullAddress, null);
            }

            case AddressingMode.AbsoluteX:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new NesIr.Memory(fullAddress, NesIr.RegisterName.XIndex);
            }

            case AddressingMode.AbsoluteY:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new NesIr.Memory(fullAddress, NesIr.RegisterName.YIndex);
            }

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }
    }
}