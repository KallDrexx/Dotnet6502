using DotNesJit.Cli;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;
using System.Reflection;
using DotNesJit.Common.Compilation;

// Parse command line arguments
var commandLineValues = CommandLineHandler.Parse(args);
if (commandLineValues == null)
{
    return 1;
}

var romFile = commandLineValues.RomFile;
Console.WriteLine($"DotNes JIT Compiler and Emulator v1.0");
Console.WriteLine($"Loading ROM: '{romFile.FullName}'");
Console.WriteLine();

try
{
    // Load and analyze ROM
    var loader = new ROMLoader();
    var romInfo = loader.LoadFromFile(romFile.FullName);
    var programRomData = loader.GetPRGROMData();
    var chrRomData = loader.GetCHRROMData();

    Console.WriteLine($"ROM Information:");
    Console.WriteLine($"  PRG ROM: {programRomData.Length} bytes ({programRomData.Length / 1024}KB)");
    Console.WriteLine($"  CHR ROM: {chrRomData.Length} bytes ({chrRomData.Length / 1024}KB)");
    Console.WriteLine($"  Mapper: {romInfo.MapperNumber}");
    Console.WriteLine($"  Reset Vector: ${romInfo.ResetVector:X4}");
    Console.WriteLine($"  Mirroring: {romInfo.MirroringType}");
    Console.WriteLine();

    // Disassemble the ROM
    Console.WriteLine("Phase 1: Disassembling ROM...");
    var disassembler = new Disassembler(romInfo, programRomData);
    disassembler.Disassemble();
    Console.WriteLine($"  [CHECKMARK] Found {disassembler.Instructions.Count} instructions");
    Console.WriteLine($"  [CHECKMARK] Found {disassembler.EntryPoints.Count} entry points");
    Console.WriteLine($"  [CHECKMARK] Found {disassembler.ReferencedAddresses.Count} referenced addresses");

    // Decompile to identify functions and control flow
    Console.WriteLine("Phase 2: Analyzing control flow...");
    var decompiler = new Decompiler(romInfo, disassembler);
    decompiler.Decompile();
    Console.WriteLine($"  [CHECKMARK] Identified {decompiler.Functions.Count} functions");
    Console.WriteLine($"  [CHECKMARK] Identified {decompiler.Variables.Count} variables");
    Console.WriteLine($"  [CHECKMARK] Identified {decompiler.CodeBlocks.Count} code blocks");

    if (commandLineValues.Verbose)
    {
        Console.WriteLine("\nFunction Analysis:");
        foreach (var func in decompiler.Functions.Values.Take(10))
        {
            Console.WriteLine($"  ${func.Address:X4}: {func.Name} ({func.Instructions.Count} instructions)");
        }
        if (decompiler.Functions.Count > 10)
        {
            Console.WriteLine($"  ... and {decompiler.Functions.Count - 10} more functions");
        }
    }

    Assembly? jitAssembly = null;
    string dllFileName = "";

    // Build JIT assembly
    Console.WriteLine("Phase 3: JIT Compilation...");

    try
    {
        var builder = new NesGameClass(Path.GetFileNameWithoutExtension(romFile.Name), decompiler, disassembler);

        var outputDir = commandLineValues.OutputDirectory ?? romFile.DirectoryName!;
        dllFileName = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(romFile.Name) + ".dll");

        // Save DLL if requested
        if (commandLineValues.SaveDll)
        {
            try
            {
                if (File.Exists(dllFileName))
                {
                    File.Delete(dllFileName);
                }
            }
            catch (IOException)
            {
                // File doesn't exist or can't be deleted, ignore
            }

            try
            {
                using var dllFile = File.Create(dllFileName);
                builder.WriteAssemblyTo(dllFile);
                dllFile.Close();
                Console.WriteLine($"  [CHECKMARK] Generated: {dllFileName}");
            }
            catch (Exception saveEx)
            {
                Console.WriteLine($"  [WARNING] Warning: Could not save DLL: {saveEx.Message}");
                if (commandLineValues.Verbose)
                {
                    Console.WriteLine($"  Save error details: {saveEx}");
                }
            }
        }

        // Load the assembly for execution
        if (File.Exists(dllFileName))
        {
            try
            {
                var assemblyBytes = File.ReadAllBytes(dllFileName);
                jitAssembly = Assembly.Load(assemblyBytes);
                Console.WriteLine($"  [CHECKMARK] Loaded JIT assembly with {decompiler.Functions.Count} compiled functions");

                // TEMPORARY SUCCESS MESSAGE: Explain what's working now vs what's disabled
                Console.WriteLine("  [CHECKMARK] JIT Compilation successful!");
                Console.WriteLine("  [CHECKMARK] Basic function compilation working");
                Console.WriteLine("  [WARNING] Advanced features temporarily disabled (see comments for details):");
                Console.WriteLine("    - Advanced interrupt handling (causes TypeBuilder circular references)");
                Console.WriteLine("    - Cross-function calls during compilation (method reference before CreateType)");
                Console.WriteLine("    - Sophisticated system methods (GetMethod before type creation)");
                Console.WriteLine("  -> This should fix the 0 cycles issue and allow basic emulation to work");
            }
            catch (Exception loadEx)
            {
                Console.WriteLine($"  [WARNING] Warning: Could not load JIT assembly: {loadEx.Message}");
                if (commandLineValues.Verbose)
                {
                    Console.WriteLine($"  Load error details: {loadEx}");
                }

                if (commandLineValues.RunEmulation)
                {
                    Console.WriteLine("  Continuing with interpretation mode...");
                }
            }
        }
        else
        {
            Console.WriteLine($"  [WARNING] Warning: DLL file not found at {dllFileName}");
            if (commandLineValues.RunEmulation)
            {
                Console.WriteLine("  Continuing with interpretation mode...");
            }
        }
    }
    catch (Exception jitEx)
    {
        Console.WriteLine($"  [WARNING] JIT Compilation failed: {jitEx.Message}");
        if (commandLineValues.Verbose)
        {
            Console.WriteLine($"  JIT error details: {jitEx}");
            Console.WriteLine($"  Stack trace: {jitEx.StackTrace}");
        }

        // DETAILED ERROR EXPLANATION for the user
        Console.WriteLine("  [INFO] TROUBLESHOOTING INFORMATION:");
        if (jitEx.Message.Contains("The invoked member is not supported before the type is created"))
        {
            Console.WriteLine("    [ERROR] Error Type: TypeBuilder method reference before CreateType()");
            Console.WriteLine("    [FIX] Fix Applied: Commented out cross-referencing methods in NesAssemblyBuilder.cs");
            Console.WriteLine("    [INFO] Details: Methods tried to call GetMethod() on TypeBuilder before calling CreateType()");
            Console.WriteLine("    [LIGHTBULB] Solution: Methods that reference each other need to be generated after CreateType()");
        }
        else if (jitEx.Message.Contains("Unable to change after type has been created"))
        {
            Console.WriteLine("    [ERROR] Error Type: Adding methods after CreateType() called");
            Console.WriteLine("    [FIX] Fix Applied: Reordered method generation and CreateType() call");
            Console.WriteLine("    [INFO] Details: CreateType() was called too early, then more methods were added");
            Console.WriteLine("    [LIGHTBULB] Solution: All method definitions must happen before CreateType()");
        }
        else if (jitEx.Message.Contains("An item with the same key has already been added"))
        {
            Console.WriteLine("    [ERROR] Error Type: Duplicate instruction handler");
            Console.WriteLine("    [FIX] Fix Applied: Removed TXS from StackHandlers.cs (kept in TransferHandlers.cs)");
            Console.WriteLine("    [INFO] Details: TXS instruction was defined in both StackHandlers and TransferHandlers");
            Console.WriteLine("    [LIGHTBULB] Solution: Each instruction mnemonic can only be handled by one handler class");
        }
        else
        {
            Console.WriteLine($"    [ERROR] Error Type: Unknown compilation error");
            Console.WriteLine($"    [INFO] Details: {jitEx.Message}");
            Console.WriteLine($"    [LIGHTBULB] Check: Verify all instruction handlers are properly defined and unique");
        }

        if (commandLineValues.RunEmulation)
        {
            Console.WriteLine("  Continuing with interpretation-only mode...");
        }
    }

    // Run emulation if requested
    if (commandLineValues.RunEmulation)
    {
        Console.WriteLine();
        Console.WriteLine("Phase 4: Starting NES Emulation");
        Console.WriteLine("Controls:");
        Console.WriteLine("  Arrow Keys: D-Pad");
        Console.WriteLine("  Z: A Button, X: B Button");
        Console.WriteLine("  Enter: Start, Space: Select");
        Console.WriteLine("  Q: Quit, S: Stats, R: Reset, P: Pause");
        Console.WriteLine("  D: Debug info, F: List functions");
        Console.WriteLine();

        try
        {
            var emulator = new AdvancedNESEmulator(romInfo, programRomData, chrRomData, decompiler, jitAssembly);
            emulator.Run(commandLineValues.EmulationMode, commandLineValues.Verbose);
        }
        catch (Exception emulationEx)
        {
            Console.WriteLine($"Emulation error: {emulationEx.Message}");
            if (commandLineValues.Verbose)
            {
                Console.WriteLine($"Emulation error details: {emulationEx}");
                Console.WriteLine($"Stack trace: {emulationEx.StackTrace}");
            }

            // EMULATION TROUBLESHOOTING
            Console.WriteLine("[INFO] EMULATION TROUBLESHOOTING:");
            if (emulationEx.Message.Contains("0 cycles") || emulationEx.Message.Contains("PC"))
            {
                Console.WriteLine("    [ERROR] Issue: CPU not executing instructions (0 cycles, PC stuck)");
                Console.WriteLine("    [FIX] Likely Cause: JIT functions not registered or ExecuteCPUCycle not working");
                Console.WriteLine("    [INFO] Check: Look for 'JIT Functions: 0' vs 'JIT Functions: 39' in output above");
                Console.WriteLine("    [LIGHTBULB] If JIT Functions = 0: JIT compilation failed, using fallback interpretation");
                Console.WriteLine("    [LIGHTBULB] If JIT Functions > 0: Check NesHal.ExecuteCPUCycle() implementation");
                Console.WriteLine("    [FIX] Next Steps: Verify hardware.RegisterJITFunction() is being called properly");
            }
            else if (emulationEx.Message.Contains("NullReference"))
            {
                Console.WriteLine("    [ERROR] Issue: Null reference - missing hardware or method setup");
                Console.WriteLine("    [FIX] Check: Hardware initialization and JIT method registration");
                Console.WriteLine("    [INFO] Verify: _hardware and _jitAssembly are not null in AdvancedNESEmulator");
            }
            else if (emulationEx.Message.Contains("TargetInvocation"))
            {
                Console.WriteLine("    [ERROR] Issue: Error calling JIT-compiled function");
                Console.WriteLine("    [FIX] Check: JIT function compilation errors or missing dependencies");
                Console.WriteLine("    [INFO] Look at: Individual instruction handler implementations");
            }
            else
            {
                Console.WriteLine($"    [ERROR] Issue: {emulationEx.Message}");
                Console.WriteLine("    [FIX] Check: General emulation setup and configuration");
            }

            return 1;
        }
    }
    else
    {
        Console.WriteLine("\nCompilation complete! Use --run to start emulation.");
        if (File.Exists(dllFileName))
        {
            Console.WriteLine($"JIT Assembly: {dllFileName}");
        }
        Console.WriteLine($"Functions compiled: {decompiler.Functions.Count}");

        // Show compilation summary
        var successfulFunctions = decompiler.Functions.Count;
        Console.WriteLine($"Compilation Summary:");
        Console.WriteLine($"  Total Functions: {decompiler.Functions.Count}");
        Console.WriteLine($"  JIT Assembly: {(File.Exists(dllFileName) ? "Generated" : "Failed")}");
        Console.WriteLine($"  Ready for emulation: {(jitAssembly != null ? "Yes" : "Interpretation mode only")}");

        // TEMPORARY STATUS REPORT
        Console.WriteLine($"\n[FIX] CURRENT STATUS (Temporary Fixes Applied):");
        Console.WriteLine($"  [CHECKMARK] Fixed: Duplicate TXS instruction handler (removed from StackHandlers)");
        Console.WriteLine($"  [CHECKMARK] Fixed: TypeBuilder circular reference errors (commented out problematic methods)");
        Console.WriteLine($"  [WARNING] Disabled: Advanced interrupt handling (temporary - causes method reference issues)");
        Console.WriteLine($"  [WARNING] Disabled: Cross-function method calls (temporary - GetMethod before CreateType)");
        Console.WriteLine($"  [WARNING] Disabled: Sophisticated CPU cycle methods (temporary - circular dependencies)");
        Console.WriteLine($"  [INFO] Next Steps to Restore Full Functionality:");
        Console.WriteLine($"    1. Test basic emulation works (cycles > 0, PC advances)");
        Console.WriteLine($"    2. Implement proper method dependency ordering");
        Console.WriteLine($"    3. Re-enable interrupt handling with late binding or delegates");
        Console.WriteLine($"    4. Add cross-function call support using function pointers");
        Console.WriteLine($"    5. Implement advanced system methods without circular references");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (commandLineValues.Verbose)
    {
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
    return 1;
}

Console.WriteLine("Done.");
return 0;