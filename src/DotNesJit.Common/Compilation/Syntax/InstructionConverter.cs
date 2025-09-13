using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation.Syntax;

/// <summary>
/// Converts a disassembled 6502 instruction into an AST instruction
/// </summary>
public static class InstructionConverter
{
    public static IReadOnlyList<NesIr.Instruction> Convert(
        DisassembledInstruction instruction,
        Disassembler disassembler,
        Decompiler decompiler)
    {
        var results = new List<NesIr.Instruction>();
        if (instruction.Label != null)
        {
            results.Add(new NesIr.Label(new NesIr.Identifier(instruction.Label)));
        }

        switch (instruction.Info.Mnemonic)
        {
            case "ADC": results.AddRange(ConvertAdc(instruction)); break;
            case "AND": results.AddRange(ConvertAnd(instruction)); break;
            case "ASL": results.AddRange(ConvertAsl(instruction)); break;
            case "BCC": results.AddRange(ConvertBcc(instruction, disassembler)); break;
            case "BCS": results.AddRange(ConvertBcs(instruction, disassembler)); break;
            case "BEQ": results.AddRange(ConvertBeq(instruction, disassembler)); break;
            case "BIT": results.AddRange(ConvertBit(instruction)); break;
            case "CLC": results.AddRange(ConvertClc()); break;
            case "CLD": results.AddRange(ConvertCld()); break;
            case "CLI": results.AddRange(ConvertCli()); break;
            case "CLV": results.AddRange(ConvertClv()); break;
            case "CMP": results.AddRange(ConvertCmp(instruction)); break;
            case "CPX": results.AddRange(ConvertCpx(instruction)); break;
            case "CPY": results.AddRange(ConvertCpy(instruction)); break;
            case "DEC": results.AddRange(ConvertDec(instruction)); break;
            case "DEX": results.AddRange(ConvertDex(instruction)); break;
            case "DEY": results.AddRange(ConvertDey(instruction)); break;
            case "EOR": results.AddRange(ConvertEor(instruction)); break;
            case "INC": results.AddRange(ConvertInc(instruction)); break;
            case "INX": results.AddRange(ConvertInx(instruction)); break;
            case "INY": results.AddRange(ConvertIny(instruction)); break;
            case "JMP": results.AddRange(ConvertJmp(instruction, disassembler)); break;
            case "JSR": results.AddRange(ConvertJsr(instruction, decompiler)); break;
            case "LDA": results.AddRange(ConvertLda(instruction)); break;
            case "LDX": results.AddRange(ConvertLdx(instruction)); break;
            case "LDY": results.AddRange(ConvertLdy(instruction)); break;
            case "LSR": results.AddRange(ConvertLsr(instruction)); break;
            case "NOP": results.AddRange(ConvertNop()); break;
            case "ORA": results.AddRange(ConvertOra(instruction)); break;
            case "PHA": results.AddRange(ConvertPha(instruction)); break;
            case "PHP": results.AddRange(ConvertPhp(instruction)); break;
            case "PLA": results.AddRange(ConvertPla()); break;
            case "PLP": results.AddRange(ConvertPlp()); break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }

        return results;
    }

    /// <summary>
    /// Add with carry
    /// </summary>
    private static NesIr.Instruction[] ConvertAdc(DisassembledInstruction instruction)
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

        var adjustForOverflow = new NesIr.WrapValueToByte(addVariable, new NesIr.Flag(NesIr.FlagName.Overflow));
        var checkForZero = ZeroFlagInstruction(addVariable);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(addVariable, isNegative);
        var storeAccumulator = new NesIr.Copy(addVariable, accumulator);

        return
        [
            firstAdd, carryAdd, adjustForOverflow, checkForZero, checkForNegative, setNegative, storeAccumulator
        ];
    }

    /// <summary>
    /// Bitwise AND
    /// </summary>
    private static NesIr.Instruction[] ConvertAnd(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var isNegative = new NesIr.Variable(1);

        var andOperation = new NesIr.Binary(NesIr.BinaryOperator.And, accumulator, operand, accumulator);
        var checkForZero = ZeroFlagInstruction(accumulator);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(accumulator, isNegative);

        return [andOperation, checkForZero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Arithmetic Shift Left
    /// </summary>
    private static NesIr.Instruction[] ConvertAsl(DisassembledInstruction instruction)
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

        var zeroFlag = ZeroFlagInstruction(operand);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(operand, tempVariable);

        return [carry, carryFlag, shift, zeroFlag, checkForNegative, setNegative];
    }

    /// <summary>
    /// Branch if carry clear
    /// </summary>
    private static NesIr.Instruction[] ConvertBcc(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfZero(new NesIr.Flag(NesIr.FlagName.Carry), target);

        return [jump];
    }

    /// <summary>
    /// Branch if carry set
    /// </summary>
    private static NesIr.Instruction[] ConvertBcs(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfNotZero(new NesIr.Flag(NesIr.FlagName.Carry), target);

        return [jump];
    }

    /// <summary>
    /// Branch if equal
    /// </summary>
    private static NesIr.Instruction[] ConvertBeq(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfNotZero(new NesIr.Flag(NesIr.FlagName.Zero), target);

        return [jump];
    }

    /// <summary>
    /// Bit test
    /// </summary>
    private static NesIr.Instruction[] ConvertBit(DisassembledInstruction instruction)
    {
        var tempVariable = new NesIr.Variable(0);
        var operand = ParseAddress(instruction);

        var andOp = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            new NesIr.Register(NesIr.RegisterName.Accumulator),
            operand,
            tempVariable);

        var zeroFlag = ZeroFlagInstruction(tempVariable);

        var overflow = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThan,
            tempVariable,
            new NesIr.Constant(255),
            new NesIr.Flag(NesIr.FlagName.Overflow));

        var (negativeFlag, setNegative) = NegativeFlagInstructions(tempVariable, tempVariable);

        return [andOp, zeroFlag, overflow, negativeFlag, setNegative];
    }

    /// <summary>
    /// Branch if minus
    /// </summary>
    private static NesIr.Instruction[] ConvertBmi(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfNotZero(new NesIr.Flag(NesIr.FlagName.Negative), target);

        return [jump];
    }

    /// <summary>
    /// Branch if not equal
    /// </summary>
    private static NesIr.Instruction[] ConvertBne(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfZero(new NesIr.Flag(NesIr.FlagName.Zero), target);

        return [jump];
    }

    /// <summary>
    /// Branch if plus
    /// </summary>
    private static NesIr.Instruction[] ConvertBpl(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfZero(new NesIr.Flag(NesIr.FlagName.Negative), target);

        return [jump];
    }

    /// <summary>
    /// Break (software IRQ)
    /// </summary>
    private static NesIr.Instruction[] ConvertBrk(DisassembledInstruction instruction)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Branch if overflow clear
    /// </summary>
    private static NesIr.Instruction[] ConvertBvc(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfZero(new NesIr.Flag(NesIr.FlagName.Overflow), target);

        return [jump];
    }

    /// <summary>
    /// Branch if overflow set
    /// </summary>
    private static NesIr.Instruction[] ConvertBvs(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.JumpIfNotZero(new NesIr.Flag(NesIr.FlagName.Overflow), target);

        return [jump];
    }

    /// <summary>
    /// Clear carry
    /// </summary>
    private static NesIr.Instruction[] ConvertClc()
    {
        var setCarry = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Carry), new NesIr.Constant(0));

        return [setCarry];
    }

    /// <summary>
    /// Clear decimal
    /// </summary>
    private static NesIr.Instruction[] ConvertCld()
    {
        var setDecimal = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Decimal), new NesIr.Constant(0));

        return [setDecimal];
    }

    /// <summary>
    /// Clear interrupt disable
    /// </summary>
    private static NesIr.Instruction[] ConvertCli()
    {
        var setInterrupt = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.InterruptDisable), new NesIr.Constant(0));

        return [setInterrupt];
    }

    /// <summary>
    /// Clear overflow
    /// </summary>
    private static NesIr.Instruction[] ConvertClv()
    {
        var setOverflow = new NesIr.Copy(new NesIr.Flag(NesIr.FlagName.Overflow), new NesIr.Constant(0));

        return [setOverflow];
    }

    /// <summary>
    /// Compare A
    /// </summary>
    private static NesIr.Instruction[] ConvertCmp(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            accumulator,
            operand,
            variable);

        var zero = ZeroFlagInstruction(accumulator);
        var carry = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            accumulator,
            operand,
            new NesIr.Flag(NesIr.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Compare X
    /// </summary>
    private static NesIr.Instruction[] ConvertCpx(DisassembledInstruction instruction)
    {
        var xIndex = new NesIr.Register(NesIr.RegisterName.XIndex);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            xIndex,
            operand,
            variable);

        var zero = ZeroFlagInstruction(xIndex);
        var carry = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            xIndex,
            operand,
            new NesIr.Flag(NesIr.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(xIndex, variable);

        return [subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Compare Y
    /// </summary>
    private static NesIr.Instruction[] ConvertCpy(DisassembledInstruction instruction)
    {
        var yIndex = new NesIr.Register(NesIr.RegisterName.YIndex);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            yIndex,
            operand,
            variable);

        var zero = ZeroFlagInstruction(yIndex);
        var carry = new NesIr.Binary(
            NesIr.BinaryOperator.GreaterThanOrEqualTo,
            yIndex,
            operand,
            new NesIr.Flag(NesIr.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(yIndex, variable);

        return [subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Decrement memory
    /// </summary>
    private static NesIr.Instruction[] ConvertDec(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            operand,
            new NesIr.Constant(1),
            variable);

        var store = new NesIr.Copy(variable, operand);
        var zero = ZeroFlagInstruction(variable);

        return [subtract, store, zero];
    }

    /// <summary>
    /// Decrement x
    /// </summary>
    private static NesIr.Instruction[] ConvertDex(DisassembledInstruction instruction)
    {
        var xIndex = new NesIr.Register(NesIr.RegisterName.XIndex);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            xIndex,
            new NesIr.Constant(1),
            variable);

        var store = new NesIr.Copy(variable, xIndex);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [subtract, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Decrement y
    /// </summary>
    private static NesIr.Instruction[] ConvertDey(DisassembledInstruction instruction)
    {
        var yIndex = new NesIr.Register(NesIr.RegisterName.YIndex);
        var variable = new NesIr.Variable(0);

        var subtract = new NesIr.Binary(
            NesIr.BinaryOperator.Subtract,
            yIndex,
            new NesIr.Constant(1),
            variable);

        var store = new NesIr.Copy(variable, yIndex);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [subtract, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Bitwise Exclusive OR
    /// </summary>
    private static NesIr.Instruction[] ConvertEor(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var variable = new NesIr.Variable(0);

        var xor = new NesIr.Binary(NesIr.BinaryOperator.Xor, accumulator, operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [xor, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment memory
    /// </summary>
    private static NesIr.Instruction[] ConvertInc(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var increment = new NesIr.Binary(NesIr.BinaryOperator.Add, operand, new NesIr.Constant(1), operand);
        var zero = ZeroFlagInstruction(operand);
        var (checkNegative, setNegative) = NegativeFlagInstructions(operand, variable);

        return [increment, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment X
    /// </summary>
    private static NesIr.Instruction[] ConvertInx(DisassembledInstruction instruction)
    {
        var xIndex = new NesIr.Register(NesIr.RegisterName.XIndex);
        var variable = new NesIr.Variable(0);

        var increment = new NesIr.Binary(NesIr.BinaryOperator.Add, xIndex, new NesIr.Constant(1), xIndex);
        var zero = ZeroFlagInstruction(xIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(xIndex, variable);

        return [increment, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment Y
    /// </summary>
    private static NesIr.Instruction[] ConvertIny(DisassembledInstruction instruction)
    {
        var yIndex = new NesIr.Register(NesIr.RegisterName.YIndex);
        var variable = new NesIr.Variable(0);

        var increment = new NesIr.Binary(NesIr.BinaryOperator.Add, yIndex, new NesIr.Constant(1), yIndex);
        var zero = ZeroFlagInstruction(yIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(yIndex, variable);

        return [increment, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Jump
    /// </summary>
    private static NesIr.Instruction[] ConvertJmp(DisassembledInstruction instruction, Disassembler disassembler)
    {
        var target = GetTargetLabel(instruction, disassembler);
        var jump = new NesIr.Jump(target);

        return [jump];
    }

    /// <summary>
    /// Jump to subroutine
    /// </summary>
    private static NesIr.Instruction[] ConvertJsr(DisassembledInstruction instruction, Decompiler decompiler)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            const string message = "JSR instruction with no target address";
            throw new InvalidOperationException(message);
        }

        if (!decompiler.Functions.TryGetValue(instruction.TargetAddress.Value, out var function))
        {
            var message = $"JSR instruction to address '{instruction.TargetAddress}' but that address is " +
                          $"not tied to a known function";

            throw new InvalidOperationException(message);
        }

        var jump = new NesIr.CallFunction(new NesIr.Identifier(function.Name));

        return [jump];
    }

    /// <summary>
    /// Load A
    /// </summary>
    private static NesIr.Instruction[] ConvertLda(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var copy = new NesIr.Copy(operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Load X
    /// </summary>
    private static NesIr.Instruction[] ConvertLdx(DisassembledInstruction instruction)
    {
        var xIndex = new NesIr.Register(NesIr.RegisterName.XIndex);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var copy = new NesIr.Copy(operand, xIndex);
        var zero = ZeroFlagInstruction(xIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(xIndex, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Load Y
    /// </summary>
    private static NesIr.Instruction[] ConvertLdy(DisassembledInstruction instruction)
    {
        var yIndex = new NesIr.Register(NesIr.RegisterName.YIndex);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var copy = new NesIr.Copy(operand, yIndex);
        var zero = ZeroFlagInstruction(yIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(yIndex, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Logical shift right
    /// </summary>
    private static NesIr.Instruction[] ConvertLsr(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var tempVariable = new NesIr.Variable(0);

        var carry = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            operand,
            new NesIr.Constant(0x01),
            tempVariable);

        var carryFlag = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            tempVariable,
            new NesIr.Constant(0x01),
            new NesIr.Flag(NesIr.FlagName.Carry));

        var shift = new NesIr.Binary(
            NesIr.BinaryOperator.ShiftRight,
            operand,
            new NesIr.Constant(1),
            operand);

        var zero = ZeroFlagInstruction(operand);
        var negative = new NesIr.Copy(new NesIr.Constant(0), new NesIr.Flag(NesIr.FlagName.Negative));

        return [carry, carryFlag, shift, zero, negative];
    }

    /// <summary>
    /// No Operation
    /// </summary>
    private static NesIr.Instruction[] ConvertNop()
    {
        return [];
    }

    /// <summary>
    /// Bitwise Or
    /// </summary>
    private static NesIr.Instruction[] ConvertOra(DisassembledInstruction instruction)
    {
        var accumulator = new NesIr.Register(NesIr.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new NesIr.Variable(0);

        var or = new NesIr.Binary(NesIr.BinaryOperator.Or, accumulator, operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [or, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Push A
    /// </summary>
    private static NesIr.Instruction[] ConvertPha(DisassembledInstruction instruction)
    {
        var push = new NesIr.PushStackValue(new NesIr.Register(NesIr.RegisterName.Accumulator));

        return [push];
    }

    /// <summary>
    /// Push processor status
    /// </summary>
    private static NesIr.Instruction[] ConvertPhp(DisassembledInstruction instruction)
    {
        var variable = new NesIr.Variable(0);

        // B flag must be set as 1
        var pullFlags = new NesIr.Copy(new NesIr.AllFlags(), variable);
        var setBit = new NesIr.Binary(
            NesIr.BinaryOperator.Or,
            variable,
            new NesIr.Constant(0b00110000),
            variable);

        var push = new NesIr.PushStackValue(variable);

        return [pullFlags, setBit, push];
    }

    /// <summary>
    /// Pull A
    /// </summary>
    private static NesIr.Instruction[] ConvertPla()
    {
        var pop = new NesIr.PopStackValue(new NesIr.Register(NesIr.RegisterName.Accumulator));

        return [pop];
    }

    /// <summary>
    /// Pull processor status
    /// </summary>
    private static NesIr.Instruction[] ConvertPlp()
    {
        var pop = new NesIr.PopStackValue(new NesIr.AllFlags());

        return [pop];
    }

    private static NesIr.Value ParseAddress(DisassembledInstruction instruction)
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

    private static NesIr.Identifier GetTargetLabel(DisassembledInstruction instruction, Disassembler disassembler)
    {
        if (instruction.TargetAddress == null ||
            !disassembler.Labels.TryGetValue(instruction.TargetAddress.Value, out var label))
        {
            var message = $"{instruction.Info.Mnemonic} instruction targeting address '{instruction.TargetAddress}' " +
                          $"but that address has no known label";

            throw new InvalidOperationException(message);
        }

        return new NesIr.Identifier(label);
    }

    /// <summary>
    /// Generates instructions to set the Negative flag based on the check instruction's 7th bit
    /// </summary>
    private static (NesIr.Instruction check, NesIr.Instruction set) NegativeFlagInstructions(
        NesIr.Value valueToCheck,
        NesIr.Variable intermediaryVariable)
    {
        var checkForNegative = new NesIr.Binary(
            NesIr.BinaryOperator.And,
            valueToCheck,
            new NesIr.Constant(0x80),
            intermediaryVariable);

        var setNegative = new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            intermediaryVariable,
            new NesIr.Constant(0x80),
            new NesIr.Flag(NesIr.FlagName.Negative));

        return (checkForNegative, setNegative);
    }

    /// <summary>
    /// Sets the zero flag based on the value passed in
    /// </summary>
    private static NesIr.Instruction ZeroFlagInstruction(NesIr.Value basedOn)
    {
        return new NesIr.Binary(
            NesIr.BinaryOperator.Equals,
            basedOn,
            new NesIr.Constant(0),
            new NesIr.Flag(NesIr.FlagName.Zero));
    }
}