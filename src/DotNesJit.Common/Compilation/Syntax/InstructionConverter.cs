using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.Syntax;

/// <summary>
/// Converts a disassembled 6502 instruction into an AST instruction
/// </summary>
public class InstructionConverter
{
    private int _variableCount;

    public NesIr.Instruction[] Convert(DisassembledInstruction instruction)
    {
        switch (instruction.Info.Mnemonic)
        {
            case "ADC":
                return ConvertAdc(instruction);

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }

    private NesIr.Instruction[] ConvertAdc(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseOperand(instruction.Info.AddressingMode, instruction.Operands);
        var addVariable = NextVariable();
        var isNewCarryVariable = NextVariable();
        var isZero = NextVariable();
        var isNegative = NextVariable();

        var firstAdd = new NesIr.Binary(NesIr.BinaryOperator.Add, accumulator, operand, addVariable);
        var carryAdd = new NesIr.Binary(
            NesIr.BinaryOperator.Add,
            addVariable,
            new NesIr.Flag(NesIr.FlagName.Carry),
            accumulator);

        var checkForOverflow = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThan,
            addVariable,
            new NesIr.Constant(255),
            isNewCarryVariable);

        var checkForZero = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            addVariable,
            new NesIr.Constant(0),
            isZero);

        var checkForNegative = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            addVariable,
            new NesIr.Constant(0x80),
            isNegative);

        var setOverflow = new NesIr.Copy(isNewCarryVariable, new NesIr.Flag(NesIr.FlagName.Carry));
        var setNegative = new NesIr.Copy(isNegative, new NesIr.Flag(NesIr.FlagName.Negative));
        var setZero = new NesIr.Copy(isZero, new NesIr.Flag(NesIr.FlagName.Zero));

        var storeAccumulator = new NesIr.Copy(addVariable, accumulator);

        return
        [
            firstAdd, carryAdd, checkForOverflow, checkForZero, checkForNegative, setOverflow, setZero,
            setNegative, storeAccumulator
        ];
    }

    private NesIr.Value ParseOperand(AddressingMode addressingMode, byte[] operands)
    {
        switch (addressingMode)
        {
            case AddressingMode.Immediate:
                return new NesIr.Constant(operands[0]);

            case AddressingMode.ZeroPage:
                return new NesIr.Memory(operands[0], null);

            case AddressingMode.ZeroPageX:
                return new NesIr.Memory(operands[0], NesIr.RegisterName.XIndex);

            case AddressingMode.ZeroPageY:
                return new NesIr.Memory(operands[0], NesIr.RegisterName.YIndex);

            case AddressingMode.Absolute:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesIr.Memory(fullAddress, null);
            }

            case AddressingMode.AbsoluteX:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesIr.Memory(fullAddress, NesIr.RegisterName.XIndex);
            }

            case AddressingMode.AbsoluteY:
            {
                var fullAddress = (ushort)((operands[1] << 8) | operands[0]);
                return new NesIr.Memory(fullAddress, NesIr.RegisterName.YIndex);
            }

            default:
                throw new NotSupportedException(addressingMode.ToString());
        }
    }

    private NesIr.Variable NextVariable()
    {
        return new NesIr.Variable($"var_{++_variableCount}");
    }
}