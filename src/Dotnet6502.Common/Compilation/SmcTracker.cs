using NESDecompiler.Core.Decompilation;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Tracks information related to code that is self modifying
/// </summary>
public class SmcTracker
{
    /// <summary>
    /// Contains a lookup of the address of all known instructions that modify their own
    /// functions, and the address that gets modified by it.
    /// </summary>
    private readonly Dictionary<ushort, ushort> _sourceInstructionToTargetMap = new();

    /// <summary>
    /// Contains a lookup of each address that is modified by self modifying code, and
    /// which source instruction triggers a change.
    /// </summary>
    private readonly Dictionary<ushort, ushort> _targetToSourceInstructionMap = new();

    public void MarkAsSelfModifying(ushort sourceInstructionAddress, ushort targetAddress)
    {
        if (!_sourceInstructionToTargetMap.TryAdd(sourceInstructionAddress, targetAddress))
        {
            // Right now we only support a single target per source instruction
            var existingTarget = _sourceInstructionToTargetMap[sourceInstructionAddress];
            if (existingTarget != targetAddress)
            {
                var message = $"Instruction at 0x{sourceInstructionAddress:X4} has modified both " +
                              $"0x{targetAddress:X4} and 0x{existingTarget:X4}";

                throw new InvalidOperationException(message);
            }

        }
        else
        {
            // source was added to lookup
            _targetToSourceInstructionMap.Add(targetAddress, sourceInstructionAddress);
        }
    }

    /// <summary>
    /// Updates the smc tracker in reaction to memory changing, possibly affecting data that
    /// is being tracked.
    /// </summary>
    public void MemoryChanged(ushort address)
    {
        // If a source instruction was changed, we no longer know if it's still self modifying.
        // NOTE: This is currently unaware if a source instruction's operand was changed, only if
        // the opcode was changed.
        if (_sourceInstructionToTargetMap.Remove(address, out var target))
        {
            _targetToSourceInstructionMap.Remove(target);
        }
    }

    /// <summary>
    /// Retrieves a list of all addresses in the specified function that are targeted by known
    /// SMC instructions.
    /// </summary>
    public HashSet<ushort> GetTargets(DecompiledFunction function)
    {
        var results = new HashSet<ushort>();
        foreach (var instruction in function.OrderedInstructions)
        {
            foreach (var offset in Enumerable.Range(0, instruction.Info.Size))
            {
                var address = (ushort)(instruction.CPUAddress + offset);
                if (_targetToSourceInstructionMap.ContainsKey(address))
                {
                    results.Add(address);
                }
            }
        }

        return results;
    }
}