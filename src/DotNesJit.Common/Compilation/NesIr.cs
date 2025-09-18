namespace DotNesJit.Common.Compilation;

/// <summary>
/// Intermediary representation of NES (6502) instructions
/// </summary>
public static class NesIr
{
    public abstract record Instruction;

    public record Copy(Value Source, Value Destination) : Instruction;

    public record Return : Instruction;

    public record Unary(UnaryOperator Operator, Value Source, Value Destination) : Instruction;

    public record Binary(BinaryOperator Operator, Value Left, Value Right, Value Destination) : Instruction;

    public record Label(Identifier Name) : Instruction;

    public record CallFunction(Identifier Name) : Instruction;

    public record Jump(Identifier Target) : Instruction;

    public record JumpIfZero(Value Condition, Identifier Target) : Instruction;

    public record JumpIfNotZero(Value Condition, Identifier Target) : Instruction;

    public record PushStackValue(Value Source) : Instruction;

    public record PopStackValue(Value Destination) : Instruction;

    public record ConvertVariableToByte(Variable Variable) : Instruction;

    public record InvokeSoftwareInterrupt : Instruction;

    public abstract record Value;

    public record Constant(byte Number) : Value;

    public record Memory(ushort Address, RegisterName? RegisterToAdd, bool SingleByteAddress) : Value;

    public record Variable(int Index) : Value;

    public record Register(RegisterName Name) : Value;

    public record Flag(FlagName FlagName) : Value;

    public record AllFlags : Value;

    public record StackPointer : Value;

    public record Identifier(string Characters);
    
    public enum RegisterName { Accumulator, XIndex, YIndex }

    public enum FlagName { Carry, Zero, InterruptDisable, BFlag, Decimal, Overflow, Negative }

    public enum UnaryOperator { BitwiseNot }

    public enum BinaryOperator
    {
        Add, Subtract, Equals, NotEquals, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo,
        And, Or, Xor, ShiftLeft, ShiftRight,
    }
}