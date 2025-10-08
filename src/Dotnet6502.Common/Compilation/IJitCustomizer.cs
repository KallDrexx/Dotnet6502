namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Allows customizing JIT operations
/// </summary>
public interface IJitCustomizer
{
    /// <summary>
    /// Updates a set of instructions that will be used to form a function
    /// </summary>
    IReadOnlyList<ConvertedInstruction> MutateInstructions(IReadOnlyList<ConvertedInstruction> instructions);

    /// <summary>
    /// A list of custom IL generators that should be used during the JIT process
    /// </summary>
    IReadOnlyDictionary<Type, MsilGenerator.CustomIlGenerator> GetCustomIlGenerators();
}