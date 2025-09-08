using System.Reflection;
using System.Reflection.Emit;
using DotNetJit.Cli.Builder.InstructionHandlers;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNetJit.Cli.Builder;

public class NesAssemblyBuilder
{
    private readonly PersistedAssemblyBuilder _builder;
    private readonly Dictionary<ushort, MethodInfo> _methods = new();
    private readonly GameClass _gameClass;
    private readonly Dictionary<string, InstructionHandler> _instructionHandlers;
    private readonly Decompiler _decompiler;

    public CpuRegisterClassBuilder Hardware { get; }

    public NesAssemblyBuilder(string namespaceName, Decompiler decompiler)
    {
        _decompiler = decompiler ?? throw new ArgumentNullException(nameof(decompiler));

        _instructionHandlers = GetType().Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract)
            .Where(x => !x.IsInterface)
            .Where(x => typeof(InstructionHandler).IsAssignableFrom(x))
            .Select(x => (InstructionHandler)Activator.CreateInstance(x)!)
            .SelectMany(x => x.Mnemonics.Select(y => new { Mnemonic = y, Instance = x }))
            .ToDictionary(x => x.Mnemonic, x => x.Instance);

        _builder = new PersistedAssemblyBuilder(
            new AssemblyName(namespaceName),
            typeof(object).Assembly);

        var rootModule = _builder.DefineDynamicModule("<Module>");
        Hardware = new CpuRegisterClassBuilder(namespaceName, rootModule);

        _gameClass = SetupGameClass(rootModule, namespaceName, decompiler);
        AddFunctions(decompiler);
        AddInterruptHandlers();

        _gameClass.Type.CreateType();
    }

    public void Save(Stream outputStream)
    {
        _builder.Save(outputStream);
    }

    private GameClass SetupGameClass(ModuleBuilder module, string namespaceName, Decompiler decompiler)
    {
        var builder = module.DefineType($"{namespaceName}.Game", TypeAttributes.Public);
        var hardwareField = builder.DefineField(
            "Hardware",
            typeof(NesHal),
            FieldAttributes.Public | FieldAttributes.Static);

        return new GameClass
        {
            Type = builder,
            CpuRegistersField = hardwareField,
            Registers = Hardware,
        };
    }

    private void AddFunctions(Decompiler decompiler)
    {
        Console.WriteLine($"Generating {decompiler.Functions.Count} functions...");

        foreach (var kvp in decompiler.Functions)
        {
            var function = kvp.Value;
            try
            {
                _methods.Add(function.Address, GenerateMethod(function, decompiler));

                if (decompiler.Functions.Count <= 20) // Only show details for small ROMs
                {
                    Console.WriteLine($"  Generated function: {function.Name} at ${function.Address:X4} " +
                                    $"({function.Instructions.Count} instructions)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to generate function {function.Name}: {ex.Message}");
            }
        }
    }

    private void AddInterruptHandlers()
    {
        // Add special interrupt handling methods
        AddInterruptCheckMethod();
        AddVBlankWaitMethod();
        AddDispatchMethod();
    }

    private void AddInterruptCheckMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "CheckInterrupts",
            MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = method.GetILGenerator();

        // Simple interrupt check - in practice this would be more sophisticated
        // TODO: Implement actual interrupt handling logic
        ilGenerator.EmitWriteLine("Checking for interrupts...");

        // Check if interrupts are enabled
        var getFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetFlag));
        var interruptDisableLabel = ilGenerator.DefineLabel();

        ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
        ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod!);
        ilGenerator.Emit(OpCodes.Brtrue, interruptDisableLabel); // Skip if interrupts disabled

        // IRQ handling would go here
        ilGenerator.EmitWriteLine("IRQ check (not implemented)");

        ilGenerator.MarkLabel(interruptDisableLabel);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private void AddVBlankWaitMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "WaitForVBlank",
            MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = method.GetILGenerator();

        ilGenerator.EmitWriteLine("Optimized VBlank wait detected");

        // In practice, this would interface with the main loop
        // For now, just add a comment
        // TODO: Implement actual VBlank wait logic
        ilGenerator.EmitWriteLine("// VBlank wait optimized - delegating to main loop");

        ilGenerator.Emit(OpCodes.Ret);
    }

    private void AddDispatchMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "DispatchToAddress",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(void),
            [typeof(ushort)]);

        var ilGenerator = method.GetILGenerator();

        ilGenerator.EmitWriteLine("Dispatching to address...");

        // Load the address parameter
        ilGenerator.Emit(OpCodes.Ldarg_0);

        // For now, just log the address - in practice this would call the appropriate function
        // TODO: implement actual dispatch logic
        ilGenerator.EmitWriteLine($"// Dispatch to address on stack");

        ilGenerator.Emit(OpCodes.Pop); // Remove address from stack
        ilGenerator.Emit(OpCodes.Ret);
    }

    private MethodBuilder GenerateMethod(NESDecompiler.Core.Decompilation.Function function, Decompiler decompiler)
    {
        var method = _gameClass.Type.DefineMethod(
            function.Name,
            MethodAttributes.Public | MethodAttributes.Static);
        var ilGenerator = method.GetILGenerator();

        try
        {
            // Add method header comment
            ilGenerator.EmitWriteLine($"=== Function: {function.Name} at ${function.Address:X4} ===");

            // Get code blocks for this function, sorted by address
            var functionBlocks = _decompiler.CodeBlocks.Values
                .Where(block => function.Instructions.Contains(block.StartAddress))
                .OrderBy(block => block.StartAddress)
                .ToList();

            if (functionBlocks.Count == 0)
            {
                ilGenerator.EmitWriteLine($"Warning: No code blocks found for function {function.Name}");
                ilGenerator.Emit(OpCodes.Ret);
                return method;
            }

            // Generate labels for all blocks that might be jump targets
            var blockLabels = new Dictionary<ushort, Label>();
            foreach (var block in functionBlocks)
            {
                if (ShouldHaveLabel(block, decompiler))
                {
                    blockLabels[block.StartAddress] = ilGenerator.DefineLabel();
                }
            }

            // Generate code for each block
            foreach (var codeBlock in functionBlocks)
            {
                // Mark label if this block has one
                if (blockLabels.TryGetValue(codeBlock.StartAddress, out var label))
                {
                    ilGenerator.MarkLabel(label);
                    ilGenerator.EmitWriteLine($"Block_{codeBlock.StartAddress:X4}:");
                }

                // Generate instructions for this block
                foreach (var instruction in codeBlock.Instructions)
                {
                    try
                    {
                        GenerateIl(ilGenerator, instruction);
                    }
                    catch (Exception ex)
                    {
                        ilGenerator.EmitWriteLine($"Error generating IL for {instruction}: {ex.Message}");
                    }
                }

                // Handle block transitions
                HandleBlockTransition(ilGenerator, codeBlock, blockLabels, decompiler);
            }

            ilGenerator.Emit(OpCodes.Ret);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating method {function.Name}: {ex.Message}");

            // Generate a minimal fallback method
            ilGenerator = method.GetILGenerator();
            ilGenerator.EmitWriteLine($"Error in function {function.Name}: {ex.Message}");
            ilGenerator.Emit(OpCodes.Ret);
        }

        return method;
    }

    private bool ShouldHaveLabel(CodeBlock block, Decompiler decompiler)
    {
        // Block should have a label if it's a target of a jump or branch
        var address = block.StartAddress;

        // Check if any instruction targets this address
        foreach (var instruction in decompiler.Disassembler.Instructions)
        {
            if (instruction.TargetAddress == address &&
                (instruction.IsBranch || instruction.IsJump))
            {
                return true;
            }
        }

        // Also add labels for function entry points
        return decompiler.Functions.ContainsKey(address);
    }

    private void HandleBlockTransition(ILGenerator ilGenerator, CodeBlock block,
        Dictionary<ushort, Label> blockLabels, Decompiler decompiler)
    {
        if (block.Instructions.Count == 0)
            return;

        var lastInstruction = block.Instructions[^1];

        // Handle different types of block endings
        if (lastInstruction.IsFunctionExit)
        {
            // RTS, RTI - function exit
            ilGenerator.EmitWriteLine("Function exit");
            // Return is handled by the method epilogue
        }
        else if (lastInstruction.Info.Mnemonic == "JMP")
        {
            // Unconditional jump
            if (lastInstruction.TargetAddress.HasValue &&
                blockLabels.TryGetValue(lastInstruction.TargetAddress.Value, out var jumpLabel))
            {
                ilGenerator.Emit(OpCodes.Br, jumpLabel);
            }
            else
            {
                ilGenerator.EmitWriteLine($"Jump to external address ${lastInstruction.TargetAddress:X4}");
            }
        }
        else if (lastInstruction.IsBranch)
        {
            // Conditional branch - this is handled by the branch instruction handler
            // but we need to ensure fall-through to the next block
            var nextAddress = (ushort)(lastInstruction.CPUAddress + lastInstruction.Info.Size);
            if (blockLabels.TryGetValue(nextAddress, out var fallThroughLabel))
            {
                // Branch handler will generate the conditional jump
                // Fall-through is automatic in IL
            }
        }
        else if (lastInstruction.Info.Mnemonic == "JSR")
        {
            // Subroutine call - fall through to next instruction
            var nextAddress = (ushort)(lastInstruction.CPUAddress + lastInstruction.Info.Size);
            if (blockLabels.TryGetValue(nextAddress, out var returnLabel))
            {
                ilGenerator.Emit(OpCodes.Br, returnLabel);
            }
        }
        // For other instructions, fall through to the next block automatically
    }

    private void GenerateIl(ILGenerator ilGenerator, DisassembledInstruction instruction)
    {
        if (!_instructionHandlers.TryGetValue(instruction.Info.Mnemonic, out var handler))
        {
            ilGenerator.EmitWriteLine($"Unsupported instruction: {instruction}");
            return;
        }

        ilGenerator.EmitWriteLine($"${instruction.CPUAddress:X4}: {instruction}");

        try
        {
            handler.Handle(ilGenerator, instruction, _gameClass);
        }
        catch (Exception ex)
        {
            ilGenerator.EmitWriteLine($"Error handling {instruction.Info.Mnemonic}: {ex.Message}");
        }
    }
}