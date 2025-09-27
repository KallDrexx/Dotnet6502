using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace Dotnet6502.Common;

/// <summary>
/// Converts a disassembled 6502 instruction into an AST instruction
/// </summary>
public static class InstructionConverter
{
    public record Context(IReadOnlyDictionary<ushort, string> Labels);

    public static IReadOnlyList<Ir6502.Instruction> Convert(
        DisassembledInstruction instruction,
        Context context)
    {
        var results = new List<Ir6502.Instruction>();
        if (instruction.Label != null)
        {
            results.Add(new Ir6502.Label(new Ir6502.Identifier(instruction.Label)));
        }

        switch (instruction.Info.Mnemonic)
        {
            case "ADC": results.AddRange(ConvertAdc(instruction)); break;
            case "AND": results.AddRange(ConvertAnd(instruction)); break;
            case "ASL": results.AddRange(ConvertAsl(instruction)); break;
            case "BCC": results.AddRange(ConvertBcc(instruction, context)); break;
            case "BCS": results.AddRange(ConvertBcs(instruction, context)); break;
            case "BEQ": results.AddRange(ConvertBeq(instruction, context)); break;
            case "BIT": results.AddRange(ConvertBit(instruction)); break;
            case "BMI": results.AddRange(ConvertBmi(instruction, context)); break;
            case "BNE": results.AddRange(ConvertBne(instruction, context)); break;
            case "BPL": results.AddRange(ConvertBpl(instruction, context)); break;
            case "BRK": results.AddRange(ConvertBrk()); break;
            case "BVC": results.AddRange(ConvertBvc(instruction, context)); break;
            case "BVS": results.AddRange(ConvertBvs(instruction, context)); break;
            case "CLC": results.AddRange(ConvertClc()); break;
            case "CLD": results.AddRange(ConvertCld()); break;
            case "CLI": results.AddRange(ConvertCli()); break;
            case "CLV": results.AddRange(ConvertClv()); break;
            case "CMP": results.AddRange(ConvertCmp(instruction)); break;
            case "CPX": results.AddRange(ConvertCpx(instruction)); break;
            case "CPY": results.AddRange(ConvertCpy(instruction)); break;
            case "DEC": results.AddRange(ConvertDec(instruction)); break;
            case "DEX": results.AddRange(ConvertDex()); break;
            case "DEY": results.AddRange(ConvertDey()); break;
            case "EOR": results.AddRange(ConvertEor(instruction)); break;
            case "INC": results.AddRange(ConvertInc(instruction)); break;
            case "INX": results.AddRange(ConvertInx()); break;
            case "INY": results.AddRange(ConvertIny()); break;
            case "JMP": results.AddRange(ConvertJmp(instruction, context)); break;
            case "JSR": results.AddRange(ConvertJsr(instruction)); break;
            case "LDA": results.AddRange(ConvertLda(instruction)); break;
            case "LDX": results.AddRange(ConvertLdx(instruction)); break;
            case "LDY": results.AddRange(ConvertLdy(instruction)); break;
            case "LSR": results.AddRange(ConvertLsr(instruction)); break;
            case "NOP": results.AddRange(ConvertNop()); break;
            case "ORA": results.AddRange(ConvertOra(instruction)); break;
            case "PHA": results.AddRange(ConvertPha()); break;
            case "PHP": results.AddRange(ConvertPhp()); break;
            case "PLA": results.AddRange(ConvertPla()); break;
            case "PLP": results.AddRange(ConvertPlp()); break;
            case "ROL": results.AddRange(ConvertRol(instruction)); break;
            case "ROR": results.AddRange(ConvertRor(instruction)); break;
            case "RTI": results.AddRange(ConvertRti()); break;
            case "RTS": results.AddRange(ConvertRts()); break;
            case "SBC": results.AddRange(ConvertSbc(instruction)); break;
            case "SEC": results.AddRange(ConvertSec()); break;
            case "SED": results.AddRange(ConvertSed()); break;
            case "SEI": results.AddRange(ConvertSei()); break;
            case "STA": results.AddRange(ConvertSta(instruction)); break;
            case "STX": results.AddRange(ConvertStx(instruction)); break;
            case "STY": results.AddRange(ConvertSty(instruction)); break;
            case "TAX": results.AddRange(ConvertTax()); break;
            case "TAY": results.AddRange(ConvertTay()); break;
            case "TSX": results.AddRange(ConvertTsx()); break;
            case "TXA": results.AddRange(ConvertTxa()); break;
            case "TXS": results.AddRange(ConvertTxs()); break;
            case "TYA": results.AddRange(ConvertTya()); break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }

        return results;
    }

    /// <summary>
    /// Add with carry
    /// </summary>
    private static Ir6502.Instruction[] ConvertAdc(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var isNegative = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(6);

        // Don't store the value in the accumulator so we don't lose track of if it overflowed due to byte precision
        var addVariable = new Ir6502.Variable(1);

        // Variables for 6502 overflow calculation: (A^result) & (M^result) & 0x80
        var originalAccumulator = new Ir6502.Variable(2);
        var aXorResult = new Ir6502.Variable(3);
        var mXorResult = new Ir6502.Variable(4);
        var overflowTemp = new Ir6502.Variable(5);

        // Preserve original accumulator value for overflow calculation
        var preserveAccumulator = new Ir6502.Copy(accumulator, originalAccumulator);
        var preserveOperand = new Ir6502.Copy(operand, operandVariable);

        var firstAdd = new Ir6502.Binary(Ir6502.BinaryOperator.Add, accumulator, operandVariable, addVariable);
        var carryAdd = new Ir6502.Binary(
            Ir6502.BinaryOperator.Add,
            addVariable,
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            addVariable);

        var setCarry = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThan,
            addVariable,
            new Ir6502.Constant(0xFF),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        // Make sure an underflow or overflow doesn't cause comparisons to be incorrect
        var convertToByte = new Ir6502.ConvertVariableToByte(addVariable);

        // Implement 6502 overflow logic: (A^result) & (M^result) & 0x80 != 0
        // A^result: XOR original accumulator with final result
        var calcAXorResult = new Ir6502.Binary(
            Ir6502.BinaryOperator.Xor,
            originalAccumulator,
            addVariable,
            aXorResult);

        // M^result: XOR operand with final result
        var calcMXorResult = new Ir6502.Binary(
            Ir6502.BinaryOperator.Xor,
            operandVariable,
            addVariable,
            mXorResult);

        // (A^result) & (M^result)
        var andXorResults = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            aXorResult,
            mXorResult,
            overflowTemp);

        // ((A^result) & (M^result)) & 0x80
        var maskSignBit = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            overflowTemp,
            new Ir6502.Constant(0x80),
            overflowTemp);

        // Set overflow flag if result equals 0x80 (signed overflow occurred)
        var setOverflow = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            overflowTemp,
            new Ir6502.Constant(0x80),
            new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var checkForZero = ZeroFlagInstruction(addVariable);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(addVariable, isNegative);
        var storeAccumulator = new Ir6502.Copy(addVariable, accumulator);

        return
        [
            preserveAccumulator, preserveOperand, firstAdd, carryAdd, setCarry, convertToByte, calcAXorResult,
            calcMXorResult, andXorResults, maskSignBit, setOverflow, checkForZero, checkForNegative, setNegative,
            storeAccumulator
        ];
    }

    /// <summary>
    /// Bitwise AND
    /// </summary>
    private static Ir6502.Instruction[] ConvertAnd(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var isNegative = new Ir6502.Variable(1);

        var andOperation = new Ir6502.Binary(Ir6502.BinaryOperator.And, accumulator, operand, accumulator);
        var checkForZero = ZeroFlagInstruction(accumulator);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(accumulator, isNegative);

        return [andOperation, checkForZero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Arithmetic Shift Left
    /// </summary>
    private static Ir6502.Instruction[] ConvertAsl(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var tempVariable = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(1);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var carry = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            operandVariable,
            new Ir6502.Constant(0x80),
            tempVariable);

        var carryFlag = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            tempVariable,
            new Ir6502.Constant(0x80),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var shift = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftLeft,
            operandVariable,
            new Ir6502.Constant(1),
            operandVariable);

        var convertToByte = new Ir6502.ConvertVariableToByte(operandVariable);
        var zeroFlag = ZeroFlagInstruction(operandVariable);
        var (checkForNegative, setNegative) = NegativeFlagInstructions(operandVariable, tempVariable);
        var storeOperand = new Ir6502.Copy(operandVariable, operand);

        return [preserveOperand, carry, carryFlag, shift, convertToByte, zeroFlag, checkForNegative, setNegative, storeOperand];
    }

    /// <summary>
    /// Branch if carry clear
    /// </summary>
    private static Ir6502.Instruction[] ConvertBcc(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfZero(new Ir6502.Flag(Ir6502.FlagName.Carry), target);

        return [jump];
    }

    /// <summary>
    /// Branch if carry set
    /// </summary>
    private static Ir6502.Instruction[] ConvertBcs(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfNotZero(new Ir6502.Flag(Ir6502.FlagName.Carry), target);

        return [jump];
    }

    /// <summary>
    /// Branch if equal
    /// </summary>
    private static Ir6502.Instruction[] ConvertBeq(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfNotZero(new Ir6502.Flag(Ir6502.FlagName.Zero), target);

        return [jump];
    }

    /// <summary>
    /// Bit test
    /// </summary>
    private static Ir6502.Instruction[] ConvertBit(DisassembledInstruction instruction)
    {
        var tempVariable = new Ir6502.Variable(0);
        var overflowTemp = new Ir6502.Variable(1);
        var operandVariable = new Ir6502.Variable(2);
        var operand = ParseAddress(instruction);

        var copy = new Ir6502.Copy(operand, operandVariable);

        var andOp = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            new Ir6502.Register(Ir6502.RegisterName.Accumulator),
            operandVariable,
            tempVariable);

        var zeroFlag = ZeroFlagInstruction(tempVariable);

        // Overflow flag is set from bit 6 of the memory operand
        var checkOverflow = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            operandVariable,
            new Ir6502.Constant(0x40),
            overflowTemp);

        var setOverflow = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            overflowTemp,
            new Ir6502.Constant(0x40),
            new Ir6502.Flag(Ir6502.FlagName.Overflow));

        // Negative flag is set from bit 7 of the memory operand
        var (negativeFlag, setNegative) = NegativeFlagInstructions(operandVariable, tempVariable);

        return [copy, andOp, zeroFlag, checkOverflow, setOverflow, negativeFlag, setNegative];
    }

    /// <summary>
    /// Branch if minus
    /// </summary>
    private static Ir6502.Instruction[] ConvertBmi(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfNotZero(new Ir6502.Flag(Ir6502.FlagName.Negative), target);

        return [jump];
    }

    /// <summary>
    /// Branch if not equal
    /// </summary>
    private static Ir6502.Instruction[] ConvertBne(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfZero(new Ir6502.Flag(Ir6502.FlagName.Zero), target);

        return [jump];
    }

    /// <summary>
    /// Branch if plus
    /// </summary>
    private static Ir6502.Instruction[] ConvertBpl(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfZero(new Ir6502.Flag(Ir6502.FlagName.Negative), target);

        return [jump];
    }

    /// <summary>
    /// Break (software IRQ)
    /// </summary>
    private static Ir6502.Instruction[] ConvertBrk()
    {
        var pushFlags = new Ir6502.PushStackValue(new Ir6502.AllFlags());
        var triggerInterrupt = new Ir6502.InvokeSoftwareInterrupt();
        var setInterruptDisable = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        var setBFlag = new Ir6502.Copy(
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.BFlag));

        return [pushFlags, setInterruptDisable, setBFlag, triggerInterrupt];
    }

    /// <summary>
    /// Branch if overflow clear
    /// </summary>
    private static Ir6502.Instruction[] ConvertBvc(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfZero(new Ir6502.Flag(Ir6502.FlagName.Overflow), target);

        return [jump];
    }

    /// <summary>
    /// Branch if overflow set
    /// </summary>
    private static Ir6502.Instruction[] ConvertBvs(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.JumpIfNotZero(new Ir6502.Flag(Ir6502.FlagName.Overflow), target);

        return [jump];
    }

    /// <summary>
    /// Clear carry
    /// </summary>
    private static Ir6502.Instruction[] ConvertClc()
    {
        var setCarry = new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Carry));

        return [setCarry];
    }

    /// <summary>
    /// Clear decimal
    /// </summary>
    private static Ir6502.Instruction[] ConvertCld()
    {
        var setDecimal = new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Decimal));

        return [setDecimal];
    }

    /// <summary>
    /// Clear interrupt disable
    /// </summary>
    private static Ir6502.Instruction[] ConvertCli()
    {
        var setInterrupt = new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        return [setInterrupt];
    }

    /// <summary>
    /// Clear overflow
    /// </summary>
    private static Ir6502.Instruction[] ConvertClv()
    {
        var setOverflow = new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Overflow));

        return [setOverflow];
    }

    /// <summary>
    /// Compare A
    /// </summary>
    private static Ir6502.Instruction[] ConvertCmp(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(1);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            accumulator,
            operandVariable,
            variable);

        var zero = ZeroFlagInstruction(variable);
        var carry = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            accumulator,
            operandVariable,
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [preserveOperand, subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Compare X
    /// </summary>
    private static Ir6502.Instruction[] ConvertCpx(DisassembledInstruction instruction)
    {
        var xIndex = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(1);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            xIndex,
            operandVariable,
            variable);

        var zero = ZeroFlagInstruction(variable);
        var carry = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            xIndex,
            operandVariable,
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [preserveOperand, subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Compare Y
    /// </summary>
    private static Ir6502.Instruction[] ConvertCpy(DisassembledInstruction instruction)
    {
        var yIndex = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(1);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            yIndex,
            operandVariable,
            variable);

        var zero = ZeroFlagInstruction(variable);
        var carry = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            yIndex,
            operandVariable,
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var (checkForNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [preserveOperand, subtract, carry, zero, checkForNegative, setNegative];
    }

    /// <summary>
    /// Decrement memory
    /// </summary>
    private static Ir6502.Instruction[] ConvertDec(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            operand,
            new Ir6502.Constant(1),
            variable);

        var store = new Ir6502.Copy(variable, operand);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [subtract, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Decrement x
    /// </summary>
    private static Ir6502.Instruction[] ConvertDex()
    {
        var xIndex = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var variable = new Ir6502.Variable(0);

        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            xIndex,
            new Ir6502.Constant(1),
            variable);

        var store = new Ir6502.Copy(variable, xIndex);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [subtract, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Decrement y
    /// </summary>
    private static Ir6502.Instruction[] ConvertDey()
    {
        var yIndex = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var variable = new Ir6502.Variable(0);

        var subtract = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            yIndex,
            new Ir6502.Constant(1),
            variable);

        var store = new Ir6502.Copy(variable, yIndex);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [subtract, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Bitwise Exclusive OR
    /// </summary>
    private static Ir6502.Instruction[] ConvertEor(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var variable = new Ir6502.Variable(0);

        var xor = new Ir6502.Binary(Ir6502.BinaryOperator.Xor, accumulator, operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [xor, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment memory
    /// </summary>
    private static Ir6502.Instruction[] ConvertInc(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var increment = new Ir6502.Binary(Ir6502.BinaryOperator.Add, operand, new Ir6502.Constant(1), variable);
        var convertToByte = new Ir6502.ConvertVariableToByte(variable);
        var store = new Ir6502.Copy(variable, operand);
        var zero = ZeroFlagInstruction(variable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(variable, variable);

        return [increment, convertToByte, store, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment X
    /// </summary>
    private static Ir6502.Instruction[] ConvertInx()
    {
        var xIndex = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var variable = new Ir6502.Variable(0);

        var increment = new Ir6502.Binary(Ir6502.BinaryOperator.Add, xIndex, new Ir6502.Constant(1), xIndex);
        var zero = ZeroFlagInstruction(xIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(xIndex, variable);

        return [increment, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Increment Y
    /// </summary>
    private static Ir6502.Instruction[] ConvertIny()
    {
        var yIndex = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var variable = new Ir6502.Variable(0);

        var increment = new Ir6502.Binary(Ir6502.BinaryOperator.Add, yIndex, new Ir6502.Constant(1), yIndex);
        var zero = ZeroFlagInstruction(yIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(yIndex, variable);

        return [increment, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Jump
    /// </summary>
    private static Ir6502.Instruction[] ConvertJmp(DisassembledInstruction instruction, Context context)
    {
        var target = GetTargetLabel(instruction, context);
        var jump = new Ir6502.Jump(target);

        return [jump];
    }

    /// <summary>
    /// Jump to subroutine
    /// </summary>
    private static Ir6502.Instruction[] ConvertJsr(DisassembledInstruction instruction)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            const string message = "JSR instruction with no target address";
            throw new InvalidOperationException(message);
        }

        var jump = new Ir6502.CallFunction(new Ir6502.TargetAddress(instruction.TargetAddress.Value));
        return [jump];
    }

    /// <summary>
    /// Load A
    /// </summary>
    private static Ir6502.Instruction[] ConvertLda(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var copy = new Ir6502.Copy(operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Load X
    /// </summary>
    private static Ir6502.Instruction[] ConvertLdx(DisassembledInstruction instruction)
    {
        var xIndex = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var copy = new Ir6502.Copy(operand, xIndex);
        var zero = ZeroFlagInstruction(xIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(xIndex, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Load Y
    /// </summary>
    private static Ir6502.Instruction[] ConvertLdy(DisassembledInstruction instruction)
    {
        var yIndex = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var copy = new Ir6502.Copy(operand, yIndex);
        var zero = ZeroFlagInstruction(yIndex);
        var (checkNegative, setNegative) = NegativeFlagInstructions(yIndex, variable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Logical shift right
    /// </summary>
    private static Ir6502.Instruction[] ConvertLsr(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var tempVariable = new Ir6502.Variable(0);
        var operandVariable = new Ir6502.Variable(1);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var carry = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            operandVariable,
            new Ir6502.Constant(0x01),
            tempVariable);

        var carryFlag = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            tempVariable,
            new Ir6502.Constant(0x01),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var shift = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftRight,
            operandVariable,
            new Ir6502.Constant(1),
            operandVariable);

        var copyToOperand = new Ir6502.Copy(operandVariable, operand);
        var zero = ZeroFlagInstruction(operandVariable);
        var negative = new Ir6502.Copy(new Ir6502.Constant(0), new Ir6502.Flag(Ir6502.FlagName.Negative));

        return [preserveOperand, carry, carryFlag, shift, zero, negative, copyToOperand];
    }

    /// <summary>
    /// No Operation
    /// </summary>
    private static Ir6502.Instruction[] ConvertNop()
    {
        return [];
    }

    /// <summary>
    /// Bitwise Or
    /// </summary>
    private static Ir6502.Instruction[] ConvertOra(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var variable = new Ir6502.Variable(0);

        var or = new Ir6502.Binary(Ir6502.BinaryOperator.Or, accumulator, operand, accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, variable);

        return [or, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Push A
    /// </summary>
    private static Ir6502.Instruction[] ConvertPha()
    {
        var push = new Ir6502.PushStackValue(new Ir6502.Register(Ir6502.RegisterName.Accumulator));

        return [push];
    }

    /// <summary>
    /// Push processor status
    /// </summary>
    private static Ir6502.Instruction[] ConvertPhp()
    {
        var variable = new Ir6502.Variable(0);

        // B flag must be set as 1
        var pullFlags = new Ir6502.Copy(new Ir6502.AllFlags(), variable);
        var setBit = new Ir6502.Binary(
            Ir6502.BinaryOperator.Or,
            variable,
            new Ir6502.Constant(0b00110000),
            variable);

        var push = new Ir6502.PushStackValue(variable);

        return [pullFlags, setBit, push];
    }

    /// <summary>
    /// Pull A
    /// </summary>
    private static Ir6502.Instruction[] ConvertPla()
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var tempVariable = new Ir6502.Variable(0);

        var pop = new Ir6502.PopStackValue(accumulator);
        var zero = ZeroFlagInstruction(accumulator);
        var (checkNegative, setNegative) = NegativeFlagInstructions(accumulator, tempVariable);

        return [pop, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Pull processor status
    /// </summary>
    private static Ir6502.Instruction[] ConvertPlp()
    {
        var pop = new Ir6502.PopStackValue(new Ir6502.AllFlags());

        return [pop];
    }

    /// <summary>
    /// Rotate left
    /// </summary>
    private static Ir6502.Instruction[] ConvertRol(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var oldCarry = new Ir6502.Variable(0);
        var tempVariable = new Ir6502.Variable(1);
        var operandVariable = new Ir6502.Variable(2);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var copyCarry = new Ir6502.Copy(new Ir6502.Flag(Ir6502.FlagName.Carry), oldCarry);
        var compareLastBit = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            operandVariable,
            new Ir6502.Constant(0x80),
            tempVariable);

        var setCarry = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            tempVariable,
            new Ir6502.Constant(0x80),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var shift = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftLeft,
            operandVariable,
            new Ir6502.Constant(1),
            operandVariable);

        var setBit0 = new Ir6502.Binary(
            Ir6502.BinaryOperator.Or,
            operandVariable,
            oldCarry,
            operandVariable);

        var convertToByte = new Ir6502.ConvertVariableToByte(operandVariable);
        var storeOperand = new Ir6502.Copy(operandVariable, operand);
        var zero = ZeroFlagInstruction(operandVariable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(operandVariable, tempVariable);

        return [
            preserveOperand, copyCarry, compareLastBit, setCarry, shift, setBit0, convertToByte, zero,
            checkNegative, setNegative, storeOperand,
        ];
    }

    /// <summary>
    /// Rotate right
    /// </summary>
    private static Ir6502.Instruction[] ConvertRor(DisassembledInstruction instruction)
    {
        var operand = ParseAddress(instruction);
        var oldCarry = new Ir6502.Variable(0);
        var tempVariable = new Ir6502.Variable(1);
        var operandVariable = new Ir6502.Variable(2);

        var preserveOperand = new Ir6502.Copy(operand, operandVariable);
        var copyCarry = new Ir6502.Copy(new Ir6502.Flag(Ir6502.FlagName.Carry), oldCarry);
        var compareLastBit = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            operandVariable,
            new Ir6502.Constant(0x01),
            tempVariable);

        var setCarry = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            tempVariable,
            new Ir6502.Constant(0x01),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        var shiftOperand = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftRight,
            operandVariable,
            new Ir6502.Constant(1),
            operandVariable);

        var shiftOldCarry = new Ir6502.Binary(
            Ir6502.BinaryOperator.ShiftLeft,
            oldCarry,
            new Ir6502.Constant(7),
            oldCarry);

        var setBit7 = new Ir6502.Binary(
            Ir6502.BinaryOperator.Or,
            operandVariable,
            oldCarry,
            operandVariable);

        var zero = ZeroFlagInstruction(operandVariable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(operandVariable, tempVariable);
        var storeOperand = new Ir6502.Copy(operandVariable, operand);

        return
        [
            preserveOperand, copyCarry, compareLastBit, setCarry, shiftOperand, shiftOldCarry, setBit7, zero,
            checkNegative, setNegative, storeOperand,
        ];
    }

    /// <summary>
    /// Return from interrupt
    /// </summary>
    private static Ir6502.Instruction[] ConvertRti()
    {
        var pop = new Ir6502.PopStackValue(new Ir6502.AllFlags());
        var ret = new Ir6502.Return();

        return [pop, ret];
    }

    /// <summary>
    /// Return from subroutine
    /// </summary>
    private static Ir6502.Instruction[] ConvertRts()
    {
        var ret = new Ir6502.Return();

        return [ret];
    }

    /// <summary>
    /// Subtract with carry
    /// </summary>
    private static Ir6502.Instruction[] ConvertSbc(DisassembledInstruction instruction)
    {
        var accumulator = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var subVariable = new Ir6502.Variable(0);
        var tempVariable = new Ir6502.Variable(1);
        var operandVariable = new Ir6502.Variable(7);

        // Variables for 6502 SBC overflow calculation: (result^A) & (result^~M) & 0x80
        var originalAccumulator = new Ir6502.Variable(2);
        var resultXorA = new Ir6502.Variable(3);
        var notMemory = new Ir6502.Variable(4);
        var resultXorNotMemory = new Ir6502.Variable(5);
        var overflowTemp = new Ir6502.Variable(6);

        // Preserve original accumulator value for overflow calculation
        var preserveAccumulator = new Ir6502.Copy(accumulator, originalAccumulator);
        var preserveOperand = new Ir6502.Copy(operand, operandVariable);

        // SBC = A - M - (1 - C)
        // Calculate (1 - C) properly
        var oneMinusCarry = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            new Ir6502.Constant(1),
            new Ir6502.Flag(Ir6502.FlagName.Carry),
            tempVariable);

        var subtractOperand = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            accumulator,
            operandVariable,
            subVariable);

        var subtractBorrow = new Ir6502.Binary(
            Ir6502.BinaryOperator.Subtract,
            subVariable,
            tempVariable,
            subVariable);

        // Carry flag is set if no borrow occurred
        // For SBC, carry is cleared (0) when borrow occurs (result < 0)
        // Since we check after arithmetic, use > 255 to detect borrow (like ADC uses > 255 for carry)
        // But SBC needs to check if result went negative, so use < 0
        var carryCheck = new Ir6502.Binary(
            Ir6502.BinaryOperator.GreaterThanOrEqualTo,
            subVariable,
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Carry));

        // Implement 6502 SBC overflow logic: (result^A) & (result^~M) & 0x80 != 0
        var calcResultXorA = new Ir6502.Binary(
            Ir6502.BinaryOperator.Xor,
            subVariable,
            originalAccumulator,
            resultXorA);

        // ~M: Bitwise NOT of operand
        var calcNotMemory = new Ir6502.Unary(
            Ir6502.UnaryOperator.BitwiseNot,
            operandVariable,
            notMemory);

        // result^~M: XOR final result with NOT of operand
        var calcResultXorNotMemory = new Ir6502.Binary(
            Ir6502.BinaryOperator.Xor,
            subVariable,
            notMemory,
            resultXorNotMemory);

        // (result^A) & (result^~M)
        var andXorResults = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            resultXorA,
            resultXorNotMemory,
            overflowTemp);

        // ((A^result) & (M^result)) & 0x80
        var maskSignBit = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            overflowTemp,
            new Ir6502.Constant(0x80),
            overflowTemp);

        // Set overflow flag if result equals 0x80 (signed overflow occurred)
        var setOverflow = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            overflowTemp,
            new Ir6502.Constant(0x80),
            new Ir6502.Flag(Ir6502.FlagName.Overflow));

        var setAccumulator = new Ir6502.Copy(subVariable, accumulator);
        var zero = ZeroFlagInstruction(subVariable);
        var (checkNegative, setNegative) = NegativeFlagInstructions(subVariable, tempVariable);

        return
        [
            preserveAccumulator, preserveOperand, oneMinusCarry, subtractOperand, subtractBorrow, carryCheck,
            calcResultXorA, calcNotMemory, calcResultXorNotMemory, andXorResults, maskSignBit, setOverflow,
            setAccumulator, zero, checkNegative, setNegative
        ];
    }

    /// <summary>
    /// Set Carry
    /// </summary>
    private static Ir6502.Instruction[] ConvertSec()
    {
        var copy = new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Carry));

        return [copy];
    }

    /// <summary>
    /// Set Decimal
    /// </summary>
    private static Ir6502.Instruction[] ConvertSed()
    {
        var copy = new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.Decimal));

        return [copy];
    }

    /// <summary>
    /// Set interrupt disable
    /// </summary>
    private static Ir6502.Instruction[] ConvertSei()
    {
        var copy = new Ir6502.Copy(new Ir6502.Constant(1), new Ir6502.Flag(Ir6502.FlagName.InterruptDisable));

        return [copy];
    }

    /// <summary>
    /// Store A
    /// </summary>
    private static Ir6502.Instruction[] ConvertSta(DisassembledInstruction instruction)
    {
        var register = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var operand = ParseAddress(instruction);
        var copy = new Ir6502.Copy(register, operand);

        return [copy];
    }

    /// <summary>
    /// Store X
    /// </summary>
    private static Ir6502.Instruction[] ConvertStx(DisassembledInstruction instruction)
    {
        var register = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var operand = ParseAddress(instruction);
        var copy = new Ir6502.Copy(register, operand);

        return [copy];
    }

    /// <summary>
    /// Store Y
    /// </summary>
    private static Ir6502.Instruction[] ConvertSty(DisassembledInstruction instruction)
    {
        var register = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var operand = ParseAddress(instruction);
        var copy = new Ir6502.Copy(register, operand);

        return [copy];
    }

    /// <summary>
    /// Transfer A To X
    /// </summary>
    private static Ir6502.Instruction[] ConvertTax()
    {
        var source = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var dest = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var copy = new Ir6502.Copy(source, dest);

        var tempVariable = new Ir6502.Variable(0);
        var zero = ZeroFlagInstruction(dest);
        var (checkNegative, setNegative) = NegativeFlagInstructions(dest, tempVariable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Transfer A To Y
    /// </summary>
    private static Ir6502.Instruction[] ConvertTay()
    {
        var source = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var dest = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var copy = new Ir6502.Copy(source, dest);

        var tempVariable = new Ir6502.Variable(0);
        var zero = ZeroFlagInstruction(dest);
        var (checkNegative, setNegative) = NegativeFlagInstructions(dest, tempVariable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Transfer stack pointer to X
    /// </summary>
    private static Ir6502.Instruction[] ConvertTsx()
    {
        var source = new Ir6502.StackPointer();
        var dest = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var copy = new Ir6502.Copy(source, dest);

        var tempVariable = new Ir6502.Variable(0);
        var zero = ZeroFlagInstruction(dest);
        var (checkNegative, setNegative) = NegativeFlagInstructions(dest, tempVariable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Transfer X To A
    /// </summary>
    private static Ir6502.Instruction[] ConvertTxa()
    {
        var source = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var dest = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var copy = new Ir6502.Copy(source, dest);

        var tempVariable = new Ir6502.Variable(0);
        var zero = ZeroFlagInstruction(dest);
        var (checkNegative, setNegative) = NegativeFlagInstructions(dest, tempVariable);

        return [copy, zero, checkNegative, setNegative];
    }

    /// <summary>
    /// Transfer X To stack pointer
    /// </summary>
    private static Ir6502.Instruction[] ConvertTxs()
    {
        var source = new Ir6502.Register(Ir6502.RegisterName.XIndex);
        var dest = new Ir6502.StackPointer();
        var copy = new Ir6502.Copy(source, dest);

        return [copy];
    }

    /// <summary>
    /// Transfer Y To A
    /// </summary>
    private static Ir6502.Instruction[] ConvertTya()
    {
        var source = new Ir6502.Register(Ir6502.RegisterName.YIndex);
        var dest = new Ir6502.Register(Ir6502.RegisterName.Accumulator);
        var copy = new Ir6502.Copy(source, dest);

        var tempVariable = new Ir6502.Variable(0);
        var zero = ZeroFlagInstruction(dest);
        var (checkNegative, setNegative) = NegativeFlagInstructions(dest, tempVariable);

        return [copy, zero, checkNegative, setNegative];
    }

    private static Ir6502.Value ParseAddress(DisassembledInstruction instruction)
    {
        switch (instruction.Info.AddressingMode)
        {
            case AddressingMode.Accumulator:
                return new Ir6502.Register(Ir6502.RegisterName.Accumulator);

            case AddressingMode.Immediate:
                return new Ir6502.Constant(instruction.Operands[0]);

            case AddressingMode.ZeroPage:
                return new Ir6502.Memory(instruction.Operands[0], null, true);

            case AddressingMode.ZeroPageX:
                return new Ir6502.Memory(instruction.Operands[0], Ir6502.RegisterName.XIndex, true);

            case AddressingMode.ZeroPageY:
                return new Ir6502.Memory(instruction.Operands[0], Ir6502.RegisterName.YIndex, true);

            case AddressingMode.Absolute:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new Ir6502.Memory(fullAddress, null, false);
            }

            case AddressingMode.AbsoluteX:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new Ir6502.Memory(fullAddress, Ir6502.RegisterName.XIndex, false);
            }

            case AddressingMode.AbsoluteY:
            {
                var fullAddress = (ushort)((instruction.Operands[1] << 8) | instruction.Operands[0]);
                return new Ir6502.Memory(fullAddress, Ir6502.RegisterName.YIndex, false);
            }

            case AddressingMode.IndexedIndirect:
                return new Ir6502.IndirectMemory(instruction.Operands[0], false);

            case AddressingMode.IndirectIndexed:
                return new Ir6502.IndirectMemory(instruction.Operands[0], true);

            default:
                throw new NotSupportedException(instruction.Info.AddressingMode.ToString());
        }
    }

    private static Ir6502.Identifier GetTargetLabel(DisassembledInstruction instruction, Context context)
    {
        if (instruction.TargetAddress == null ||
            !context.Labels.TryGetValue(instruction.TargetAddress.Value, out var label))
        {
            var message = $"{instruction.Info.Mnemonic} instruction targeting address '{instruction.TargetAddress}' " +
                          $"but that address has no known label";

            throw new InvalidOperationException(message);
        }

        return new Ir6502.Identifier(label);
    }

    /// <summary>
    /// Generates instructions to set the Negative flag based on the check instruction's 7th bit
    /// </summary>
    private static (Ir6502.Instruction check, Ir6502.Instruction set) NegativeFlagInstructions(
        Ir6502.Value valueToCheck,
        Ir6502.Variable intermediaryVariable)
    {
        var checkForNegative = new Ir6502.Binary(
            Ir6502.BinaryOperator.And,
            valueToCheck,
            new Ir6502.Constant(0x80),
            intermediaryVariable);

        var setNegative = new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            intermediaryVariable,
            new Ir6502.Constant(0x80),
            new Ir6502.Flag(Ir6502.FlagName.Negative));

        return (checkForNegative, setNegative);
    }

    /// <summary>
    /// Sets the zero flag based on the value passed in
    /// </summary>
    private static Ir6502.Instruction ZeroFlagInstruction(Ir6502.Value basedOn)
    {
        return new Ir6502.Binary(
            Ir6502.BinaryOperator.Equals,
            basedOn,
            new Ir6502.Constant(0),
            new Ir6502.Flag(Ir6502.FlagName.Zero));
    }
}