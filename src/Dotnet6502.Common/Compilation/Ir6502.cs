namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Intermediary representation to support 6502 instructions
/// </summary>
public static class Ir6502
{
    public abstract record Instruction;

    public abstract record JumpTarget;

    public interface ICallTarget;

    public record Copy(Value Source, Value Destination) : Instruction;

    public record Return(Variable VariableWithReturnAddress) : Instruction;

    public record Unary(UnaryOperator Operator, Value Source, Value Destination) : Instruction;

    public record Binary(BinaryOperator Operator, Value Left, Value Right, Value Destination) : Instruction;

    public record Label(Identifier Name) : Instruction;

    public record CallFunction(ICallTarget CallTarget) : Instruction;

    public record Jump(Identifier Target) : Instruction;

    public record JumpIfZero(Value Condition, Identifier Target) : Instruction;

    public record JumpIfNotZero(Value Condition, Identifier Target) : Instruction;

    public record PushStackValue(Value Source) : Instruction;

    public record PopStackValue(Value Destination) : Instruction;

    public record ConvertVariableToByte(Variable Variable) : Instruction;

    /// <summary>
    /// Performs a poll for interrupt, and redirects to the correct interrupt implementation if
    /// told to. Is automatically inserted and should not be manually inserted in normal circumstances.
    /// </summary>
    /// <param name="ContinuationAddress"></param>
    public record PollForInterrupt(ushort ContinuationAddress) : Instruction;

    /// <summary>
    /// Adds the specified text into a location that's visible while debugging
    /// </summary>
    public record StoreDebugString(string Text) : Instruction;

    public record DebugValue(Value ValueToLog) : Instruction;

    public record NoOp : Instruction;

    public abstract record Value;

    public record Constant(byte Number) : Value;

    public record Memory(ushort Address, RegisterName? RegisterToAdd, bool SingleByteAddress) : Value;

    public record IndirectMemory(byte ZeroPageAddress, bool IsPreIndexed, bool IsPostIndexed) : Value;

    public record Variable(int Index) : Value, ICallTarget;

    public record Register(RegisterName Name) : Value;

    public record Flag(FlagName FlagName) : Value;

    public record AllFlags : Value;

    public record StackPointer : Value;

    public record Identifier(string Characters) : JumpTarget;

    public record FunctionAddress(ushort Address, bool IsIndirect) : ICallTarget;

    public enum RegisterName { Accumulator, XIndex, YIndex }

    public enum FlagName { Carry, Zero, InterruptDisable, BFlag, Decimal, Overflow, Negative }

    public enum UnaryOperator { BitwiseNot, LogicalNot }

    public enum BinaryOperator
    {
        Add, Subtract, Equals, NotEquals, GreaterThan, GreaterThanOrEqualTo, LessThan, LessThanOrEqualTo,
        And, Or, Xor, ShiftLeft, ShiftRight,
    }
}