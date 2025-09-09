using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Allows generating MSIL for a single instruction
/// </summary>
public abstract class InstructionHandler
{
    /// <summary>
    /// The Mneumonic this handler can generate MSIL for
    /// </summary>
    public abstract string[] Mnemonics { get; }

    /// <summary>
    /// Generates MSIL for the specified instruction
    /// </summary>
    public void Handle(
        ILGenerator ilGenerator,
        DisassembledInstruction instruction,
        GameClass gameClass)
    {
        if (!Mnemonics.Contains(instruction.Info.Mnemonic))
        {
            var message = $"Instruction handler {GetType().FullName} cannot handle instructions " +
                          $"with the mnemonic of '{instruction.Info.Mnemonic}'";

            throw new ArgumentException(message);
        }

        HandleInternal(ilGenerator, instruction, gameClass);
    }

    protected abstract void HandleInternal(
        ILGenerator ilGenerator,
        DisassembledInstruction instruction,
        GameClass gameClass);
}