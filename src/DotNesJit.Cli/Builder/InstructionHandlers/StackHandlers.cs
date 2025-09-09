using System.Reflection.Emit;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Cli.Builder.InstructionHandlers;

/// <summary>
/// Handlers for stack operations (PHA, PLA, PHP, PLP, etc.)
/// 
/// NOTE: TXS (Transfer X to Stack Pointer) has been REMOVED from this handler
/// REASON: TXS was duplicated in both StackHandlers and TransferHandlers, causing
/// "An item with the same key has already been added. Key: TXS" error in NesAssemblyBuilder
/// 
/// TXS is correctly implemented in TransferHandlers.cs since it's a register transfer
/// instruction, not a stack operation instruction. Stack operations are for pushing/pulling
/// values to/from the stack, while TXS just copies X register to stack pointer.
/// </summary>
public class StackHandlers : InstructionHandler
{
    // FIXED: Removed "TXS" from this array - it belongs in TransferHandlers
    // ORIGINAL: public override string[] Mnemonics => ["TXS"];
    // REASON: TXS is a transfer instruction (Transfer X to Stack pointer), not a stack
    // operation like PHA/PLA. It was causing duplicate key errors.
    public override string[] Mnemonics => ["PHA", "PLA", "PHP", "PLP"];

    protected override void HandleInternal(ILGenerator ilGenerator, DisassembledInstruction instruction, GameClass gameClass)
    {
        switch (instruction.Info.Mnemonic)
        {
            // REMOVED: TXS case - now handled in TransferHandlers.cs
            // case "TXS":
            //     // Copy the value at the X register to the stack pointer
            //     ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.XIndex);
            //     ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.StackPointer);
            //     break;

            case "PHA":
                // Push accumulator to stack
                ilGenerator.EmitWriteLine("Push accumulator to stack");

                // Get current stack pointer
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);

                // Load stack base address (0x0100)
                ilGenerator.Emit(OpCodes.Ldc_I4, 0x0100);

                // Add stack pointer to get actual stack address
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.StackPointer);
                ilGenerator.Emit(OpCodes.Add);

                // Load accumulator value to push
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.Accumulator);

                // Call WriteMemory(address, value)
                var writeMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.WriteMemory));
                if (writeMemoryMethod != null)
                {
                    ilGenerator.Emit(OpCodes.Callvirt, writeMemoryMethod);
                }

                // Decrement stack pointer
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.StackPointer);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Sub);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
                ilGenerator.Emit(OpCodes.And);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.StackPointer);
                break;

            case "PLA":
                // Pull accumulator from stack
                ilGenerator.EmitWriteLine("Pull accumulator from stack");

                // Increment stack pointer first
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.Registers.StackPointer);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Add);
                ilGenerator.Emit(OpCodes.Ldc_I4, 0xFF);
                ilGenerator.Emit(OpCodes.And);
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.StackPointer);

                // Get stack address
                ilGenerator.Emit(OpCodes.Ldc_I4, 0x0100);
                ilGenerator.Emit(OpCodes.Add);

                var addressLocal = ilGenerator.DeclareLocal(typeof(ushort));
                ilGenerator.Emit(OpCodes.Stloc, addressLocal);

                // Read from memory
                ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Ldloc, addressLocal); // Load address
                var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
                if (readMemoryMethod != null)
                {
                    ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);
                }

                // Store in accumulator and update flags
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Stsfld, gameClass.Registers.Accumulator);

                // Update zero and negative flags
                IlUtils.UpdateZeroFlag(gameClass, ilGenerator);
                IlUtils.UpdateNegativeFlag(gameClass, ilGenerator);
                break;

            case "PHP":
                // Push processor status to stack
                ilGenerator.EmitWriteLine("Push processor status to stack");

                // Get processor status from hardware
                var getStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProcessorStatus));
                if (getStatusMethod != null)
                {
                    ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                    ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);

                    var statusLocal = ilGenerator.DeclareLocal(typeof(byte));
                    ilGenerator.Emit(OpCodes.Stloc, statusLocal);

                    // Push to stack using hardware method
                    var pushStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushStack));
                    if (pushStackMethod != null)
                    {
                        ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                        ilGenerator.Emit(OpCodes.Ldloc, statusLocal); // Load status value
                        ilGenerator.Emit(OpCodes.Callvirt, pushStackMethod);
                    }
                }
                break;

            case "PLP":
                // Pull processor status from stack
                ilGenerator.EmitWriteLine("Pull processor status from stack");

                var pullStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PullStack));
                var setStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetProcessorStatus));

                if (pullStackMethod != null && setStatusMethod != null)
                {
                    // Pull value from stack
                    ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                    ilGenerator.Emit(OpCodes.Callvirt, pullStackMethod);

                    var statusLocal = ilGenerator.DeclareLocal(typeof(byte));
                    ilGenerator.Emit(OpCodes.Stloc, statusLocal);

                    // Set processor status
                    ilGenerator.Emit(OpCodes.Ldsfld, gameClass.CpuRegistersField);
                    ilGenerator.Emit(OpCodes.Ldloc, statusLocal); // Load status value
                    ilGenerator.Emit(OpCodes.Callvirt, setStatusMethod);
                }
                break;

            default:
                throw new NotSupportedException(instruction.Info.Mnemonic);
        }
    }
}