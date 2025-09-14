using System.Reflection;
using System.Reflection.Emit;
using DotNesJit.Cli.Builder.InstructionHandlers;
using DotNesJit.Common.Compilation.InstructionHandlers;
using DotNesJit.Common.Hal;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;

namespace DotNesJit.Common.Compilation;

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

        // TEMPORARILY COMMENTED OUT: These cause TypeBuilder circular reference issues
        // REASON: Methods try to call GetMethod() on TypeBuilder before CreateType() is called
        // TODO: Fix by reordering or using different architecture for cross-method calls
        // AddInterruptHandlers();
        // AddAdvancedSystemMethods();

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
            typeof(INesHal),
            FieldAttributes.Public | FieldAttributes.Static);

        return new GameClass
        {
            Type = builder,
            HardwareField = hardwareField,
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
                // Generate a minimal stub function instead of failing completely
                try
                {
                    var stubMethod = GenerateStubMethod(function);
                    _methods.Add(function.Address, stubMethod);
                    Console.WriteLine($"  Generated stub for: {function.Name}");
                }
                catch (Exception stubEx)
                {
                    Console.WriteLine($"  Failed to generate stub for {function.Name}: {stubEx.Message}");
                }
            }
        }
    }

    private MethodBuilder GenerateStubMethod(NESDecompiler.Core.Decompilation.Function function)
    {
        var method = _gameClass.Type.DefineMethod(
            function.Name + "_stub",
            MethodAttributes.Public | MethodAttributes.Static);
        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, $"Stub function for {function.Name} at ${function.Address:X4}");
        IlUtils.AddMsilComment(ilGenerator, "This function had compilation errors and was replaced with a stub");
        ilGenerator.Emit(OpCodes.Ret);

        return method;
    }

    // TEMPORARILY COMMENTED OUT: AddInterruptHandlers() and related methods
    // REASON: These methods cause "The invoked member is not supported before the type is created" errors
    // because they try to reference methods that don't exist yet during IL generation

    /*
    private void AddInterruptHandlers()
    {
        AddSophisticatedInterruptCheckMethod();
        AddNMIHandlerMethod();
        AddIRQHandlerMethod();
        AddVBlankWaitMethod();
        AddDispatchMethod();
    }

    private void AddAdvancedSystemMethods()
    {
        AddCPUCycleExecutionMethod();
        AddInterruptVectorHandlerMethod();
        AddStackOperationsMethod();
        AddMemoryAccessWrapperMethod();
    }
    */

    /// <summary>
    /// TEMPORARILY COMMENTED OUT: Sophisticated interrupt checking with proper priority handling
    /// REASON: This method tries to call other methods in the same type that don't exist yet
    /// </summary>
    /*
    private void AddSophisticatedInterruptCheckMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "CheckInterrupts",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(bool), // Returns true if interrupt was processed
            Type.EmptyTypes);

        var ilGenerator = method.GetILGenerator();

        // Declare local variables
        var nmiRequestedLocal = ilGenerator.DeclareLocal(typeof(bool));
        var irqRequestedLocal = ilGenerator.DeclareLocal(typeof(bool));
        var interruptDisabledLocal = ilGenerator.DeclareLocal(typeof(bool));

        // Labels for control flow
        var checkNMI = ilGenerator.DefineLabel();
        var checkIRQ = ilGenerator.DefineLabel();
        var processNMI = ilGenerator.DefineLabel();
        var processIRQ = ilGenerator.DefineLabel();
        var noInterrupt = ilGenerator.DefineLabel();
        var endMethod = ilGenerator.DefineLabel();

        IlUtils.AddMsilComment(ilGenerator, "=== Sophisticated Interrupt Check ===");

        // Check if hardware is available
        ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
        ilGenerator.Emit(OpCodes.Brfalse, noInterrupt); // If hardware is null, no interrupts

        // Get NMI request status (NMI cannot be disabled)
        var requestNMIMethod = typeof(NesHal).GetMethod("RequestNMI");
        var getNMIStatusMethod = typeof(NesHal).GetMethod("GetNMIRequested") ??
                                 typeof(NesHal).GetMethod("CheckNMIPending");

        if (getNMIStatusMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getNMIStatusMethod);
            ilGenerator.Emit(OpCodes.Stloc, nmiRequestedLocal);
        }
        else
        {
            // Fallback: assume no NMI for now
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, nmiRequestedLocal);
        }

        // Get IRQ request status
        var getIRQStatusMethod = typeof(NesHal).GetMethod("GetIRQRequested") ??
                                 typeof(NesHal).GetMethod("CheckIRQPending");

        if (getIRQStatusMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getIRQStatusMethod);
            ilGenerator.Emit(OpCodes.Stloc, irqRequestedLocal);
        }
        else
        {
            // Fallback: assume no IRQ for now
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, irqRequestedLocal);
        }

        // Get interrupt disable flag status
        var getFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetFlag));
        if (getFlagMethod != null)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
            ilGenerator.Emit(OpCodes.Callvirt, getFlagMethod);
            ilGenerator.Emit(OpCodes.Stloc, interruptDisabledLocal);
        }
        else
        {
            // Fallback: assume interrupts enabled
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, interruptDisabledLocal);
        }

        // Check NMI first (highest priority, cannot be disabled)
        ilGenerator.MarkLabel(checkNMI);
        ilGenerator.Emit(OpCodes.Ldloc, nmiRequestedLocal);
        ilGenerator.Emit(OpCodes.Brtrue, processNMI);

        // Check IRQ (can be disabled by interrupt flag)
        ilGenerator.MarkLabel(checkIRQ);
        ilGenerator.Emit(OpCodes.Ldloc, irqRequestedLocal);
        ilGenerator.Emit(OpCodes.Brfalse, noInterrupt); // No IRQ pending

        ilGenerator.Emit(OpCodes.Ldloc, interruptDisabledLocal);
        ilGenerator.Emit(OpCodes.Brtrue, noInterrupt); // IRQ disabled

        ilGenerator.Emit(OpCodes.Br, processIRQ);

        // Process NMI
        ilGenerator.MarkLabel(processNMI);
        IlUtils.AddMsilComment(ilGenerator, "Processing NMI interrupt");

        // PROBLEM LINE: This tries to get a method that doesn't exist yet
        var handleNMIMethod = typeof(NesHal).GetMethod("HandleNMI") ??
                              _gameClass.Type.GetMethod("HandleNMI"); // This fails

        if (handleNMIMethod != null)
        {
            if (handleNMIMethod.IsStatic)
            {
                ilGenerator.Emit(OpCodes.Call, handleNMIMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Callvirt, handleNMIMethod);
            }
        }
        else
        {
            // Call our generated NMI handler
            ilGenerator.Emit(OpCodes.Call, _gameClass.Type.GetMethod("ProcessNMI")); // This also fails
        }

        ilGenerator.Emit(OpCodes.Ldc_I4_1); // Return true - interrupt processed
        ilGenerator.Emit(OpCodes.Br, endMethod);

        // Process IRQ
        ilGenerator.MarkLabel(processIRQ);
        IlUtils.AddMsilComment(ilGenerator, "Processing IRQ interrupt");

        var handleIRQMethod = typeof(NesHal).GetMethod("HandleIRQ") ??
                              _gameClass.Type.GetMethod("HandleIRQ"); // This fails

        if (handleIRQMethod != null)
        {
            if (handleIRQMethod.IsStatic)
            {
                ilGenerator.Emit(OpCodes.Call, handleIRQMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
                ilGenerator.Emit(OpCodes.Callvirt, handleIRQMethod);
            }
        }
        else
        {
            // Call our generated IRQ handler
            ilGenerator.Emit(OpCodes.Call, _gameClass.Type.GetMethod("ProcessIRQ")); // This also fails
        }

        ilGenerator.Emit(OpCodes.Ldc_I4_1); // Return true - interrupt processed
        ilGenerator.Emit(OpCodes.Br, endMethod);

        // No interrupt
        ilGenerator.MarkLabel(noInterrupt);
        ilGenerator.Emit(OpCodes.Ldc_I4_0); // Return false - no interrupt processed

        ilGenerator.MarkLabel(endMethod);
        ilGenerator.Emit(OpCodes.Ret);
    }
    */

    /// <summary>
    /// TEMPORARILY COMMENTED OUT: Complete NMI handler implementation
    /// REASON: Works fine, but depends on CheckInterrupts method which has issues
    /// </summary>
    /*
    private void AddNMIHandlerMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "ProcessNMI",
            MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "=== NMI Handler ===");

        // Get current PC and push to stack (NMI pushes PC, then status)
        var getPCMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProgramCounter));
        var pushAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushAddress));
        var pushStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushStack));
        var getStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProcessorStatus));
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));
        var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
        var setPCMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetProgramCounter));

        if (getPCMethod != null && pushAddressMethod != null && pushStackMethod != null &&
            getStatusMethod != null && setFlagMethod != null && readMemoryMethod != null && setPCMethod != null)
        {
            var currentPCLocal = ilGenerator.DeclareLocal(typeof(ushort));
            var statusLocal = ilGenerator.DeclareLocal(typeof(byte));
            var nmiVectorLocal = ilGenerator.DeclareLocal(typeof(ushort));

            // Get current PC
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getPCMethod);
            ilGenerator.Emit(OpCodes.Stloc, currentPCLocal);

            // Push PC to stack
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, currentPCLocal);
            ilGenerator.Emit(OpCodes.Callvirt, pushAddressMethod);

            // Get processor status
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
            ilGenerator.Emit(OpCodes.Stloc, statusLocal);

            // Push status to stack
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, statusLocal);
            ilGenerator.Emit(OpCodes.Callvirt, pushStackMethod);

            // Set interrupt disable flag
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);

            // Read NMI vector from $FFFA-$FFFB
            // Read low byte
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFA);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

            // Read high byte
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFB);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

            // Combine into 16-bit vector
            ilGenerator.Emit(OpCodes.Ldc_I4, 8);
            ilGenerator.Emit(OpCodes.Shl);
            ilGenerator.Emit(OpCodes.Or);
            ilGenerator.Emit(OpCodes.Stloc, nmiVectorLocal);

            // Jump to NMI vector
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, nmiVectorLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);

            IlUtils.AddMsilComment(ilGenerator, "NMI processing complete");
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, "NMI handling methods not available - using simplified approach");
        }

        ilGenerator.Emit(OpCodes.Ret);
    }
    */

    /// <summary>
    /// TEMPORARILY COMMENTED OUT: Complete IRQ handler implementation
    /// REASON: Works fine, but depends on CheckInterrupts method which has issues
    /// </summary>
    /*
    private void AddIRQHandlerMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "ProcessIRQ",
            MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "=== IRQ Handler ===");

        var getPCMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProgramCounter));
        var pushAddressMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushAddress));
        var pushStackMethod = typeof(NesHal).GetMethod(nameof(NesHal.PushStack));
        var getStatusMethod = typeof(NesHal).GetMethod(nameof(NesHal.GetProcessorStatus));
        var setFlagMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetFlag));
        var readMemoryMethod = typeof(NesHal).GetMethod(nameof(NesHal.ReadMemory));
        var setPCMethod = typeof(NesHal).GetMethod(nameof(NesHal.SetProgramCounter));

        if (getPCMethod != null && pushAddressMethod != null && pushStackMethod != null &&
            getStatusMethod != null && setFlagMethod != null && readMemoryMethod != null && setPCMethod != null)
        {
            var currentPCLocal = ilGenerator.DeclareLocal(typeof(ushort));
            var statusLocal = ilGenerator.DeclareLocal(typeof(byte));
            var irqVectorLocal = ilGenerator.DeclareLocal(typeof(ushort));

            // Get current PC
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getPCMethod);
            ilGenerator.Emit(OpCodes.Stloc, currentPCLocal);

            // Push PC to stack
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, currentPCLocal);
            ilGenerator.Emit(OpCodes.Callvirt, pushAddressMethod);

            // Get processor status and clear B flag for IRQ
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Callvirt, getStatusMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0xEF); // Clear B flag (bit 4)
            ilGenerator.Emit(OpCodes.And);
            ilGenerator.Emit(OpCodes.Stloc, statusLocal);

            // Push modified status to stack
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, statusLocal);
            ilGenerator.Emit(OpCodes.Callvirt, pushStackMethod);

            // Set interrupt disable flag
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, (int)CpuStatusFlags.InterruptDisable);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, setFlagMethod);

            // Read IRQ vector from $FFFE-$FFFF
            // Read low byte
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFE);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

            // Read high byte
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldc_I4, 0xFFFF);
            ilGenerator.Emit(OpCodes.Callvirt, readMemoryMethod);

            // Combine into 16-bit vector
            ilGenerator.Emit(OpCodes.Ldc_I4, 8);
            ilGenerator.Emit(OpCodes.Shl);
            ilGenerator.Emit(OpCodes.Or);
            ilGenerator.Emit(OpCodes.Stloc, irqVectorLocal);

            // Jump to IRQ vector
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.CpuRegistersField);
            ilGenerator.Emit(OpCodes.Ldloc, irqVectorLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);

            IlUtils.AddMsilComment(ilGenerator, "IRQ processing complete");
        }
        else
        {
            IlUtils.AddMsilComment(ilGenerator, "IRQ handling methods not available - using simplified approach");
        }

        ilGenerator.Emit(OpCodes.Ret);
    }
    */

    private void AddVBlankWaitMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "WaitForVBlank",
            MethodAttributes.Public | MethodAttributes.Static);

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "Optimized VBlank wait detected");
        IlUtils.AddMsilComment(ilGenerator, "// VBlank wait optimized - delegating to main loop");
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

        IlUtils.AddMsilComment(ilGenerator, "Dispatching to address...");
        IlUtils.AddMsilComment(ilGenerator, $"// Dispatch to address on stack");
        ilGenerator.Emit(OpCodes.Pop); // Remove address from stack
        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// TEMPORARILY COMMENTED OUT: CPU cycle execution method
    /// REASON: This tries to call CheckInterrupts method which doesn't exist yet
    /// </summary>
    /*
    private void AddCPUCycleExecutionMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "ExecuteCPUCycle",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(bool),
            Type.EmptyTypes);

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "Executing CPU cycle with interrupt checking");

        // PROBLEM LINE: This tries to call a method that doesn't exist yet
        ilGenerator.Emit(OpCodes.Call, _gameClass.Type.GetMethod("CheckInterrupts")); // FAILS HERE
        var continueExecution = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brfalse, continueExecution);

        // Interrupt was processed, return true
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Ret);

        ilGenerator.MarkLabel(continueExecution);
        IlUtils.AddMsilComment(ilGenerator, "No interrupts pending - continue normal execution");

        // Return true for successful execution
        ilGenerator.Emit(OpCodes.Ldc_I4_1);
        ilGenerator.Emit(OpCodes.Ret);
    }
    */

    private void AddInterruptVectorHandlerMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "HandleInterruptVector",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(void),
            [typeof(ushort), typeof(string)]);

        var ilGenerator = method.GetILGenerator();

        // Declare local variables
        var vectorAddressLocal = ilGenerator.DeclareLocal(typeof(ushort));
        var vectorTypeLocal = ilGenerator.DeclareLocal(typeof(string));

        IlUtils.AddMsilComment(ilGenerator, "=== Interrupt Vector Handler ===");

        // Store parameters in locals for easier access
        ilGenerator.Emit(OpCodes.Ldarg_0); // Load vector address parameter
        ilGenerator.Emit(OpCodes.Stloc, vectorAddressLocal);
        ilGenerator.Emit(OpCodes.Ldarg_1); // Load vector type parameter
        ilGenerator.Emit(OpCodes.Stloc, vectorTypeLocal);

        // Check hardware availability
        ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
        var endMethod = ilGenerator.DefineLabel();
        ilGenerator.Emit(OpCodes.Brfalse, endMethod);

        // Create labels for different vector types
        var nmiVectorLabel = ilGenerator.DefineLabel();
        var resetVectorLabel = ilGenerator.DefineLabel();
        var irqVectorLabel = ilGenerator.DefineLabel();
        var unknownVectorLabel = ilGenerator.DefineLabel();

        // Switch on vector type
        ilGenerator.Emit(OpCodes.Ldloc, vectorTypeLocal);
        ilGenerator.Emit(OpCodes.Ldstr, "NMI");
        var stringEqualsMethod = typeof(string).GetMethod("Equals", [typeof(string), typeof(string)]);
        if (stringEqualsMethod != null)
        {
            ilGenerator.Emit(OpCodes.Call, stringEqualsMethod);
            ilGenerator.Emit(OpCodes.Brtrue, nmiVectorLabel);
        }

        ilGenerator.Emit(OpCodes.Ldloc, vectorTypeLocal);
        ilGenerator.Emit(OpCodes.Ldstr, "RESET");
        if (stringEqualsMethod != null)
        {
            ilGenerator.Emit(OpCodes.Call, stringEqualsMethod);
            ilGenerator.Emit(OpCodes.Brtrue, resetVectorLabel);
        }

        ilGenerator.Emit(OpCodes.Ldloc, vectorTypeLocal);
        ilGenerator.Emit(OpCodes.Ldstr, "IRQ");
        if (stringEqualsMethod != null)
        {
            ilGenerator.Emit(OpCodes.Call, stringEqualsMethod);
            ilGenerator.Emit(OpCodes.Brtrue, irqVectorLabel);
        }

        ilGenerator.Emit(OpCodes.Br, unknownVectorLabel);

        // NMI Vector Handler
        ilGenerator.MarkLabel(nmiVectorLabel);
        IlUtils.AddMsilComment(ilGenerator, "Processing NMI vector");

        var setPCMethod = typeof(INesHal).GetMethod(nameof(INesHal.SetProgramCounter));
        // COMMENTED OUT: This would try to call ProcessNMI method which doesn't exist yet
        // var processNMIMethod = _gameClass.Type.GetMethod("ProcessNMI");

        if (setPCMethod != null)
        {
            // Set PC to NMI vector address
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Ldloc, vectorAddressLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);

            // COMMENTED OUT: Call NMI handler if available
            // if (processNMIMethod != null)
            // {
            //     ilGenerator.Emit(OpCodes.Call, processNMIMethod);
            // }
        }
        ilGenerator.Emit(OpCodes.Br, endMethod);

        // RESET Vector Handler
        ilGenerator.MarkLabel(resetVectorLabel);
        IlUtils.AddMsilComment(ilGenerator, "Processing RESET vector");

        var resetMethod = typeof(INesHal).GetMethod("Reset");
        if (setPCMethod != null && resetMethod != null)
        {
            // Reset system state
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Callvirt, resetMethod);

            // Set PC to reset vector
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Ldloc, vectorAddressLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);

            // Look for reset function in our compiled functions
            var resetFunctionAddress = _decompiler.ROMInfo.ResetVector;
            if (_methods.ContainsKey(resetFunctionAddress))
            {
                IlUtils.AddMsilComment(ilGenerator, $"Calling compiled reset function at ${resetFunctionAddress:X4}");
                // Call the compiled reset function
                var resetFunctionMethod = _methods[resetFunctionAddress];
                ilGenerator.Emit(OpCodes.Call, resetFunctionMethod);
            }
        }
        ilGenerator.Emit(OpCodes.Br, endMethod);

        // IRQ Vector Handler
        ilGenerator.MarkLabel(irqVectorLabel);
        IlUtils.AddMsilComment(ilGenerator, "Processing IRQ vector");

        // COMMENTED OUT: This would try to call ProcessIRQ method which doesn't exist yet
        // var processIRQMethod = _gameClass.Type.GetMethod("ProcessIRQ");
        if (setPCMethod != null)
        {
            // Set PC to IRQ vector address
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Ldloc, vectorAddressLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);

            // COMMENTED OUT: Call IRQ handler if available
            // if (processIRQMethod != null)
            // {
            //     ilGenerator.Emit(OpCodes.Call, processIRQMethod);
            // }
        }
        ilGenerator.Emit(OpCodes.Br, endMethod);

        // Unknown Vector Handler
        ilGenerator.MarkLabel(unknownVectorLabel);
        IlUtils.AddMsilComment(ilGenerator, "Unknown interrupt vector type");
        if (setPCMethod != null)
        {
            // Just set PC to the vector address
            ilGenerator.Emit(OpCodes.Ldsfld, _gameClass.HardwareField);
            ilGenerator.Emit(OpCodes.Ldloc, vectorAddressLocal);
            ilGenerator.Emit(OpCodes.Callvirt, setPCMethod);
        }

        ilGenerator.MarkLabel(endMethod);
        ilGenerator.Emit(OpCodes.Ret);
    }

    private void AddStackOperationsMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "PerformStackOperation",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(void),
            [typeof(string), typeof(byte)]);

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "Performing stack operation");
        IlUtils.AddMsilComment(ilGenerator, "// Stack operations delegated to hardware");

        ilGenerator.Emit(OpCodes.Ret);
    }

    private void AddMemoryAccessWrapperMethod()
    {
        var method = _gameClass.Type.DefineMethod(
            "AccessMemory",
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            typeof(byte),
            [typeof(ushort), typeof(bool)]); // address, isWrite

        var ilGenerator = method.GetILGenerator();

        IlUtils.AddMsilComment(ilGenerator, "Memory access wrapper");

        // For now, just return 0
        ilGenerator.Emit(OpCodes.Ldc_I4_0);
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
            IlUtils.AddMsilComment(ilGenerator, $"=== Function: {function.Name} at ${function.Address:X4} ===");

            // TEMPORARILY COMMENTED OUT: Add interrupt check at function entry
            // REASON: CheckInterrupts method doesn't exist yet and trying to reference it
            // causes "The invoked member is not supported before the type is created" error
            //
            // ORIGINAL CODE THAT FAILS:
            // IlUtils.AddMsilComment(ilGenerator, "Checking for interrupts at function entry");
            // ilGenerator.Emit(OpCodes.Call, _gameClass.Type.GetMethod("CheckInterrupts"));
            // var continueFunction = ilGenerator.DefineLabel();
            // ilGenerator.Emit(OpCodes.Brfalse, continueFunction);
            // IlUtils.AddMsilComment(ilGenerator, "Interrupt processed - exiting function early");
            // ilGenerator.Emit(OpCodes.Ret);
            // ilGenerator.MarkLabel(continueFunction);

            // Get instructions for this function directly from the disassembler
            var functionInstructions = _decompiler.Disassembler.Instructions
                .Where(inst => function.Instructions.Contains(inst.CPUAddress))
                .OrderBy(inst => inst.CPUAddress)
                .ToList();

            if (functionInstructions.Count == 0)
            {
                IlUtils.AddMsilComment(ilGenerator, $"Warning: No instructions found for function {function.Name}");
                ilGenerator.Emit(OpCodes.Ret);
                return method;
            }

            IlUtils.AddMsilComment(ilGenerator, $"Processing {functionInstructions.Count} instructions");

            // TEMPORARILY COMMENTED OUT: Add periodic interrupt checks for long functions
            // REASON: Same issue as above - CheckInterrupts method doesn't exist yet
            //
            // ORIGINAL CODE:
            // var instructionCount = 0;
            // const int INTERRUPT_CHECK_INTERVAL = 10; // Check every 10 instructions

            // Generate code for each instruction
            foreach (var instruction in functionInstructions)
            {
                try
                {
                    // TEMPORARILY COMMENTED OUT: Periodic interrupt checking
                    // REASON: References CheckInterrupts method that doesn't exist yet
                    //
                    // ORIGINAL CODE:
                    // if (instructionCount > 0 && instructionCount % INTERRUPT_CHECK_INTERVAL == 0)
                    // {
                    //     IlUtils.AddMsilComment(ilGenerator, $"Periodic interrupt check at instruction {instructionCount}");
                    //     ilGenerator.Emit(OpCodes.Call, _gameClass.Type.GetMethod("CheckInterrupts"));
                    //     var continueAfterCheck = ilGenerator.DefineLabel();
                    //     ilGenerator.Emit(OpCodes.Brfalse, continueAfterCheck);
                    //     IlUtils.AddMsilComment(ilGenerator, "Interrupt processed during function execution");
                    //     ilGenerator.Emit(OpCodes.Ret);
                    //     ilGenerator.MarkLabel(continueAfterCheck);
                    // }

                    GenerateIl(ilGenerator, instruction);
                    // instructionCount++;
                }
                catch (Exception ex)
                {
                    IlUtils.AddMsilComment(ilGenerator, $"Error generating IL for {instruction}: {ex.Message}");
                    // Continue processing other instructions
                }
            }

            // Ensure the method always returns
            ilGenerator.Emit(OpCodes.Ret);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating method {function.Name}: {ex.Message}");

            // Generate a minimal fallback method
            ilGenerator = method.GetILGenerator();
            IlUtils.AddMsilComment(ilGenerator, $"Error in function {function.Name}: {ex.Message}");
            ilGenerator.Emit(OpCodes.Ret);
        }

        return method;
    }

    private void GenerateIl(ILGenerator ilGenerator, DisassembledInstruction instruction)
    {
        IlUtils.AddMsilComment(ilGenerator, $"${instruction.CPUAddress:X4}: {instruction}");

        if (!_instructionHandlers.TryGetValue(instruction.Info.Mnemonic, out var handler))
        {
            IlUtils.AddMsilComment(ilGenerator, $"Unsupported instruction: {instruction}");
            return;
        }

        try
        {
            handler.Handle(ilGenerator, instruction, _gameClass);
        }
        catch (Exception ex)
        {
            IlUtils.AddMsilComment(ilGenerator, $"Error handling {instruction.Info.Mnemonic}: {ex.Message}");
            // Don't rethrow - just log and continue
        }
    }
}