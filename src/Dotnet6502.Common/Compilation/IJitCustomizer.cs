using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Allows customizing JIT operations
/// </summary>
public interface IJitCustomizer<THal> where THal : Base6502Hal
{
    /// <summary>
    /// Updates a set of instructions that will be used to form a function
    /// </summary>
    IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions);

    /// <summary>
    /// A list of custom IL generators that should be used during the JIT process
    /// </summary>
    IReadOnlyDictionary<Type, MsilGenerator<THal>.CustomIlGenerator> GetCustomIlGenerators();

    /// <summary>
    /// Allows adding additional instructions to the interpreter that are used by
    /// this JitCustomizer.
    /// </summary>
    void AddInstructions(Ir6502Interpreter interpreter);
}
