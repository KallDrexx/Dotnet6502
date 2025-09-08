using System.Reflection;
using System.Reflection.Emit;
using NESDecompiler.Core.CPU;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handles branch instructions with VBlank waiting pattern detection - FIXED VERSION
/// </summary>
public class BranchHandlers : InstructionHandler
{
    public override string[] Mnemonics => ["BCC", "BCS", "BEQ", "BMI", "BNE", "BPL", "BVC", "BVS"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        if (!instruction.TargetAddress.HasValue)
        {
            // Skip branches with unknown targets
            ilGenerator.EmitWriteLine($"Skipping branch with unknown target: {instruction}");
            return;
        }

        var targetAddress = instruction.TargetAddress.Value;
        var flagToCheck = GetFlagForBranch(instruction.Info.Mnemonic);
        var shouldBranchIfSet = ShouldBranchIfFlagSet(instruction.Info.Mnemonic);

        // Check if this might be a VBlank waiting loop
        if (IsVBlankWaitingPattern(instruction, targetAddress))
        {
            GenerateVBlankWaitingCode(ilGenerator, instruction, gameClass);
        }
        else
        {
            GenerateNormalBranchCode(ilGenerator, instruction, gameClass, flagToCheck, shouldBranchIfSet, targetAddress);
        }
    }

    /// <summary>
    /// Detects if this branch instruction is part of a VBlank waiting pattern
    /// </summary>
    private bool IsVBlankWaitingPattern(DisassembledInstruction instruction, ushort targetAddress)
    {
        // Common VBlank waiting patterns:
        // 1. BPL (branch if positive) after LDA $2002 - waiting for bit 7 to be set
        // 2. BEQ/BNE after checking VBlank flag

        if (instruction.Info.Mnemonic == "BPL")
        {
            // Check if target is a few bytes back (typical tight loop)
            int offset = targetAddress - instruction.CPUAddress;
            if (offset >= -10 && offset <= 0)
            {
                return true; // Likely VBlank waiting loop
            }
        }

        if (instruction.Info.Mnemonic == "BNE" || instruction.Info.Mnemonic == "BEQ")
        {
            // Similar check for other branch types
            int offset = targetAddress - instruction.CPUAddress;
            if (offset >= -15 && offset <= 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Generates optimized code for VBlank waiting patterns
    /// </summary>
    private void GenerateVBlankWaitingCode(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        ilGenerator.EmitWriteLine($"VBlank waiting pattern detected: {instruction}");

        // Instead of a tight loop, call the main loop's VBlank detection
        var waitForVBlankMethod = typeof(NesHal).GetMethod("WaitForVBlank");

        if (waitForVBlankMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, waitForVBlankMethod);
        }
        else
        {
            // Fallback: generate normal branch but add a comment
            ilGenerator.EmitWriteLine("// VBlank wait - consider optimizing");
            var flagToCheck = GetFlagForBranch(instruction.Info.Mnemonic);
            var shouldBranchIfSet = ShouldBranchIfFlagSet(instruction.Info.Mnemonic);
            GenerateNormalBranchCode(ilGenerator, instruction, gameClass, flagToCheck, shouldBranchIfSet, instruction.TargetAddress.Value);
        }
    }

    /// <summary>
    /// Generates normal branch code with proper flag checking
    /// </summary>
    private void GenerateNormalBranchCode(ILGenerator ilGenerator, DisassembledInstruction instruction,
        GameClass gameClass, CpuStatusFlags flagToCheck, bool shouldBranchIfSet, ushort targetAddress)
    {
        // Get flag checking method
        var getFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetFlag));
        if (getFlagMethod == null)
        {
            ilGenerator.EmitWriteLine($"Error: GetFlag method not found for branch {instruction.Info.Mnemonic}");
            return;
        }

        // Create labels for branch logic
        var branchTakenLabel = ilGenerator.DefineLabel();
        var branchNotTakenLabel = ilGenerator.DefineLabel();

        // Load hardware instance and flag enum
        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)flagToCheck);
        ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);

        // Check if we should branch
        if (shouldBranchIfSet)
        {
            ilGenerator.Emit(OpCodes.Brtrue, branchTakenLabel);
        }
        else
        {
            ilGenerator.Emit(OpCodes.Brfalse, branchTakenLabel);
        }

        // Branch not taken - continue to next instruction
        ilGenerator.MarkLabel(branchNotTakenLabel);
        ilGenerator.EmitWriteLine($"Branch not taken - continuing to next instruction");
        ilGenerator.Emit(OpCodes.Br, GetEndLabel(ilGenerator));

        // Branch taken - jump to target
        ilGenerator.MarkLabel(branchTakenLabel);
        ilGenerator.EmitWriteLine($"Branch taken to ${targetAddress:X4}");

        // For now, we'll use a simple approach and call a dispatch method
        GenerateBranchTarget(ilGenerator, gameClass, targetAddress);

        MarkEndLabel(ilGenerator);
    }

    private Label GetEndLabel(ILGenerator ilGenerator)
    {
        // Create a unique end label for this branch
        return ilGenerator.DefineLabel();
    }

    private void MarkEndLabel(ILGenerator ilGenerator)
    {
        var endLabel = ilGenerator.DefineLabel();
        ilGenerator.MarkLabel(endLabel);
    }

    /// <summary>
    /// Generates code to handle branch targets
    /// </summary>
    private void GenerateBranchTarget(ILGenerator ilGenerator, GameClass gameClass, ushort targetAddress)
    {
        // For simplicity, we'll use a dispatch method
        var dispatchMethod = typeof(NesHal).GetMethod("DispatchToAddress");

        if (dispatchMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)targetAddress);
            ilGenerator.Emit(OpCodes.Callvirt, dispatchMethod);
        }
        else
        {
            // Fallback: just add a comment about where we should jump
            ilGenerator.EmitWriteLine($"// Should jump to address ${targetAddress:X4}");
        }
    }

    /// <summary>
    /// Gets the CPU flag that should be checked for the given branch instruction
    /// </summary>
    private CpuStatusFlags GetFlagForBranch(string mnemonic)
    {
        return mnemonic switch
        {
            "BCC" or "BCS" => CpuStatusFlags.Carry,
            "BEQ" or "BNE" => CpuStatusFlags.Zero,
            "BMI" or "BPL" => CpuStatusFlags.Negative,
            "BVC" or "BVS" => CpuStatusFlags.Overflow,
            _ => throw new NotSupportedException($"Unknown branch instruction: {mnemonic}")
        };
    }

    /// <summary>
    /// Determines if the branch should be taken when the flag is set (true) or clear (false)
    /// </summary>
    private bool ShouldBranchIfFlagSet(string mnemonic)
    {
        return mnemonic switch
        {
            "BCS" => true,  // Branch if carry set
            "BCC" => false, // Branch if carry clear
            "BEQ" => true,  // Branch if equal (zero set)
            "BNE" => false, // Branch if not equal (zero clear)
            "BMI" => true,  // Branch if minus (negative set)
            "BPL" => false, // Branch if plus (negative clear)
            "BVS" => true,  // Branch if overflow set
            "BVC" => false, // Branch if overflow clear
            _ => throw new NotSupportedException($"Unknown branch instruction: {mnemonic}")
        };
    }
}