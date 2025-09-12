namespace DotNesJit.Common.Compilation.Syntax;

/// <summary>
/// Intermediary representation of NES (6502) instructions
/// </summary>
public static class NesIr
{
    public abstract record Instruction;

    public record Function(string Name, ushort Address, IReadOnlyList<Instruction> Instructions);

    public record Copy(Value Source, Value Destination) : Instruction;

    public record Return : Instruction;

    public record Binary(BinaryOperator Operator, Value Left, Value Right, Value Destination) : Instruction;

    public abstract record Value;

    public record Constant(byte Number) : Value;

    public record Memory(ushort Address, RegisterName? RegisterToAdd) : Value;

    public record Variable(string Name) : Value;

    public record Register(RegisterName Name) : Value;

    public record Flag(FlagName FlagName) : Value;
    
    public enum RegisterName { Accumulator, XIndex, YIndex }

    public enum FlagName { Carry, Zero, InterruptDisable, BFlag, Decimal, Overflow, Negative }

    public enum BinaryOperator
    {
        Add, Subtract, Equals, NotEquals, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo,
        And, Or, Xor,
    }
}