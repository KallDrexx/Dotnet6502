using DotNetJit.Cli;
using DotNetJit.Cli.Builder;
using DotNetJit.Cli.Emulation;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.Disassembly;
using NESDecompiler.Core.ROM;
using System;
using System.IO;
using System.Reflection;

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
    Console.WriteLine($"  ✓ Found {disassembler.Instructions.Count} instructions");
    Console.WriteLine($"  ✓ Found {disassembler.EntryPoints.Count} entry points");
    Console.WriteLine($"  ✓ Found {disassembler.ReferencedAddresses.Count} referenced addresses");

    // Decompile to identify functions and control flow
    Console.WriteLine("Phase 2: Analyzing control flow...");
    var decompiler = new Decompiler(romInfo, disassembler);
    decompiler.Decompile();
    Console.WriteLine($"  ✓ Identified {decompiler.Functions.Count} functions");
    Console.WriteLine($"  ✓ Identified {decompiler.Variables.Count} variables");
    Console.WriteLine($"  ✓ Identified {decompiler.CodeBlocks.Count} code blocks");

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
        var builder = new NesAssemblyBuilder(Path.GetFileNameWithoutExtension(romFile.Name), decompiler);

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
                builder.Save(dllFile);
                dllFile.Close();
                Console.WriteLine($"  ✓ Generated: {dllFileName}");
            }
            catch (Exception saveEx)
            {
                Console.WriteLine($"  ⚠ Warning: Could not save DLL: {saveEx.Message}");
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
                Console.WriteLine($"  ✓ Loaded JIT assembly with {decompiler.Functions.Count} compiled functions");
            }
            catch (Exception loadEx)
            {
                Console.WriteLine($"  ⚠ Warning: Could not load JIT assembly: {loadEx.Message}");
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
            Console.WriteLine($"  ⚠ Warning: DLL file not found at {dllFileName}");
            if (commandLineValues.RunEmulation)
            {
                Console.WriteLine("  Continuing with interpretation mode...");
            }
        }
    }
    catch (Exception jitEx)
    {
        Console.WriteLine($"  ⚠ JIT Compilation failed: {jitEx.Message}");
        if (commandLineValues.Verbose)
        {
            Console.WriteLine($"  JIT error details: {jitEx}");
            Console.WriteLine($"  Stack trace: {jitEx.StackTrace}");
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

/// <summary>
/// Advanced NES emulator with full JIT integration and enhanced error handling
/// </summary>
public class AdvancedNESEmulator
{
    private readonly ROMInfo _romInfo;
    private readonly byte[] _prgRom;
    private readonly byte[] _chrRom;
    private readonly Decompiler _decompiler;
    private readonly Assembly? _jitAssembly;
    private NesHal _hardware;
    private NESMainLoop _mainLoop;
    private readonly Dictionary<ushort, MethodInfo> _jitMethods = new();
    private bool _isPaused = false;
    private DateTime _startTime;

    public AdvancedNESEmulator(ROMInfo romInfo, byte[] prgRom, byte[] chrRom,
        Decompiler decompiler, Assembly? jitAssembly)
    {
        _romInfo = romInfo;
        _prgRom = prgRom;
        _chrRom = chrRom;
        _decompiler = decompiler;
        _jitAssembly = jitAssembly;
    }

    public void Run(string mode, bool verbose)
    {
        try
        {
            SetupEmulation();

            _startTime = DateTime.UtcNow;

            // Run emulation in background thread
            var emulationThread = new System.Threading.Thread(() => RunEmulationLoop(mode, verbose))
            {
                IsBackground = true,
                Name = "NES Emulation"
            };

            emulationThread.Start();

            // Handle user input on main thread
            HandleUserInput(verbose);

            // Clean shutdown
            _mainLoop.Stop();
            if (emulationThread.IsAlive)
            {
                emulationThread.Join(5000);
            }

            var totalTime = DateTime.UtcNow - _startTime;
            Console.WriteLine($"\nEmulation ran for {totalTime:mm\\:ss}");
            ShowFinalStats();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Emulation error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    private void SetupEmulation()
    {
        // Create hardware abstraction layer
        _hardware = new NesHal(_prgRom, _chrRom);

        // Create main loop
        _mainLoop = new NESMainLoop(_hardware);
        _hardware.SetMainLoop(_mainLoop);

        // Set initial PC to reset vector
        _hardware.SetProgramCounter(_romInfo.ResetVector);

        // Setup JIT functions if available
        if (_jitAssembly != null)
        {
            SetupJITFunctions();
        }

        // Configure main loop delegates
        _mainLoop.CPUStep = ExecuteCPUStep;
        _mainLoop.NMIHandler = ExecuteNMIHandler;
        _mainLoop.FrameComplete = OnFrameComplete;
        _mainLoop.VBlankWaitDetection = DetectVBlankWaiting;

        Console.WriteLine($"Emulation setup complete:");
        Console.WriteLine($"  Reset Vector: ${_romInfo.ResetVector:X4}");
        Console.WriteLine($"  JIT Functions: {_jitMethods.Count}");
        Console.WriteLine($"  Mode: Advanced with full hardware emulation");
    }

    private void SetupJITFunctions()
    {
        if (_jitAssembly == null) return;

        try
        {
            var gameType = _jitAssembly.GetTypes()
                .FirstOrDefault(t => t.Name == "Game" || t.Name.EndsWith(".Game"));

            if (gameType != null)
            {
                var hardwareField = gameType.GetField("Hardware", BindingFlags.Public | BindingFlags.Static);
                hardwareField?.SetValue(null, _hardware);

                var methods = gameType.GetMethods(BindingFlags.Public | BindingFlags.Static);

                foreach (var function in _decompiler.Functions.Values)
                {
                    var method = methods.FirstOrDefault(m =>
                        m.Name == function.Name ||
                        m.Name.Contains(function.Address.ToString("X4")));

                    if (method != null)
                    {
                        _jitMethods[function.Address] = method;

                        // Register with enhanced error handling
                        _hardware.RegisterJITFunction(function.Address, () =>
                        {
                            try
                            {
                                method.Invoke(null, null);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                // Unwrap the exception completely
                                var realException = ex;
                                while (realException.InnerException != null)
                                    realException = realException.InnerException;

                                Console.WriteLine($"DETAILED JIT ERROR in {function.Name} at ${function.Address:X4}:");
                                Console.WriteLine($"  Exception Type: {realException.GetType().FullName}");
                                Console.WriteLine($"  Message: {realException.Message}");
                                Console.WriteLine($"  Source: {realException.Source}");

                                if (realException.StackTrace != null)
                                {
                                    Console.WriteLine("  Stack Trace:");
                                    var lines = realException.StackTrace.Split('\n').Take(5);
                                    foreach (var line in lines)
                                    {
                                        Console.WriteLine($"    {line.Trim()}");
                                    }
                                }

                                return false;
                            }
                        }, function.Name);
                    }
                }

                Console.WriteLine($"  ✓ Mapped {_jitMethods.Count} JIT functions");
            }
            else
            {
                Console.WriteLine("  ⚠ Could not find Game class in JIT assembly");
                var types = _jitAssembly.GetTypes();
                Console.WriteLine($"  Available types: {string.Join(", ", types.Select(t => t.Name))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ JIT setup error: {ex.Message}");
        }
    }

    private void RunEmulationLoop(string mode, bool verbose)
    {
        try
        {
            var modeDisplayName = mode switch
            {
                "event-driven" => "Event-Driven (Recommended)",
                "cycle-accurate" => "Cycle-Accurate (Slow but precise)",
                "instruction-based" => "Instruction-Based (Fast)",
                "hybrid" => "Hybrid (Adaptive)",
                _ => "Event-Driven (Default)"
            };

            Console.WriteLine($"Starting emulation in {modeDisplayName} mode...");

            switch (mode.ToLower())
            {
                case "event-driven":
                    _mainLoop.RunEventDriven();
                    break;
                case "cycle-accurate":
                    _mainLoop.RunCycleAccurate();
                    break;
                case "instruction-based":
                    _mainLoop.RunInstructionBased();
                    break;
                case "hybrid":
                    _mainLoop.RunHybrid();
                    break;
                default:
                    Console.WriteLine("Unknown mode, using event-driven...");
                    _mainLoop.RunEventDriven();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Emulation loop error: {ex.Message}");
            if (verbose)
            {
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    private void ExecuteCPUStep()
    {
        if (_isPaused)
        {
            System.Threading.Thread.Sleep(100);
            return;
        }

        // Execute one CPU cycle using JIT or interpretation
        _hardware.ExecuteCPUCycle();
    }

    private void ExecuteNMIHandler()
    {
        // Try to find and execute NMI handler function
        var vectors = _hardware.GetInterruptVectors();

        if (_jitMethods.TryGetValue(vectors.nmi, out var nmiMethod))
        {
            try
            {
                nmiMethod.Invoke(null, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NMI handler error: {ex.Message}");
            }
        }
        else
        {
            // Fallback: call hardware's NMI handler
            _hardware.HandleNMI();
        }
    }

    private void OnFrameComplete()
    {
        // Update input, handle any frame-based logic
        if (!_isPaused)
        {
            // Simulate frame timing
            System.Threading.Thread.Sleep(1);
        }
    }

    private bool DetectVBlankWaiting()
    {
        // Only detect VBlank waiting if we see a very specific pattern
        var pc = _hardware.GetProgramCounter();

        // Check for the classic VBlank wait: LDA $2002, BPL loop
        if (pc >= 0x8000 && pc < 0xFFFC)
        {
            try
            {
                var inst1 = _hardware.ReadMemory(pc);
                var inst2 = _hardware.ReadMemory((ushort)(pc + 1));
                var inst3 = _hardware.ReadMemory((ushort)(pc + 2));
                var inst4 = _hardware.ReadMemory((ushort)(pc + 3));

                // LDA $2002 (AD 02 20) followed by BPL (10)
                if (inst1 == 0xAD && inst2 == 0x02 && inst3 == 0x20 && inst4 == 0x10)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        return false;
    }

    private void HandleUserInput(bool verbose)
    {
        var controller = NESController.None;

        Console.WriteLine("Emulation started. Press any key for controls...");

        while (_mainLoop.GetStats().IsRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                // Reset controller state
                controller = NESController.None;

                // Handle emulation control keys
                switch (char.ToLower(key.KeyChar))
                {
                    case 'q':
                        Console.WriteLine("Quitting emulation...");
                        _mainLoop.Stop();
                        return;

                    case 's':
                        ShowDetailedStats();
                        continue;

                    case 'r':
                        Console.WriteLine("Resetting system...");
                        ResetSystem();
                        continue;

                    case 'p':
                        _isPaused = !_isPaused;
                        Console.WriteLine(_isPaused ? "Emulation paused" : "Emulation resumed");
                        continue;

                    case 'd':
                        ShowDebugInfo();
                        continue;

                    case 'f':
                        ShowFunctionInfo();
                        continue;

                    case 'h':
                        ShowHelp();
                        continue;
                }

                // Handle game controller input
                switch (key.Key)
                {
                    case ConsoleKey.Z:
                        controller |= NESController.A;
                        break;
                    case ConsoleKey.X:
                        controller |= NESController.B;
                        break;
                    case ConsoleKey.Enter:
                        controller |= NESController.Start;
                        break;
                    case ConsoleKey.Spacebar:
                        controller |= NESController.Select;
                        break;
                    case ConsoleKey.UpArrow:
                        controller |= NESController.Up;
                        break;
                    case ConsoleKey.DownArrow:
                        controller |= NESController.Down;
                        break;
                    case ConsoleKey.LeftArrow:
                        controller |= NESController.Left;
                        break;
                    case ConsoleKey.RightArrow:
                        controller |= NESController.Right;
                        break;
                }

                // Update controller state
                _hardware.SetControllerState(1, controller);
            }

            System.Threading.Thread.Sleep(50);
        }
    }

    private void ResetSystem()
    {
        _mainLoop.Reset();
        _hardware.Reset();
        Console.WriteLine($"System reset - PC: ${_romInfo.ResetVector:X4}");
    }

    private void ShowDetailedStats()
    {
        var stats = _mainLoop.GetStats();
        var runtime = DateTime.UtcNow - _startTime;

        Console.WriteLine("\n=== Detailed Emulation Statistics ===");
        Console.WriteLine($"Runtime: {runtime:hh\\:mm\\:ss}");
        Console.WriteLine($"Total Cycles: {stats.TotalCycles:N0}");
        Console.WriteLine($"Cycles/Second: {stats.CyclesPerSecond:N0}");
        Console.WriteLine($"Current FPS: {stats.CurrentFPS:F1}");
        Console.WriteLine($"Current Scanline: {stats.CurrentScanline}");
        Console.WriteLine($"VBlank Detections: {stats.VBlankDetections}");
        Console.WriteLine($"JIT Functions: {_jitMethods.Count}");
        Console.WriteLine($"Paused: {_isPaused}");
        Console.WriteLine($"CPU State: {_hardware.GetCPUState()}");
        Console.WriteLine($"PPU State: {_hardware.GetPPUStatus()}");
        Console.WriteLine($"Interrupt State: {_hardware.GetInterruptState()}");
        Console.WriteLine("=====================================\n");
    }

    private void ShowDebugInfo()
    {
        var pc = _hardware.GetProgramCounter();
        var sp = _hardware.GetStackPointer();
        var status = _hardware.GetProcessorStatus();

        Console.WriteLine("\n=== Debug Information ===");
        Console.WriteLine($"Program Counter: ${pc:X4}");
        Console.WriteLine($"Stack Pointer: ${sp:X2}");
        Console.WriteLine($"Processor Status: ${status:X2} (Binary: {Convert.ToString(status, 2).PadLeft(8, '0')})");

        // Show flags
        Console.WriteLine("Flags:");
        Console.WriteLine($"  Carry: {_hardware.GetFlag(CpuStatusFlags.Carry)}");
        Console.WriteLine($"  Zero: {_hardware.GetFlag(CpuStatusFlags.Zero)}");
        Console.WriteLine($"  Interrupt Disable: {_hardware.GetFlag(CpuStatusFlags.InterruptDisable)}");
        Console.WriteLine($"  Decimal: {_hardware.GetFlag(CpuStatusFlags.Decimal)}");
        Console.WriteLine($"  Overflow: {_hardware.GetFlag(CpuStatusFlags.Overflow)}");
        Console.WriteLine($"  Negative: {_hardware.GetFlag(CpuStatusFlags.Negative)}");

        // Show memory around PC
        Console.WriteLine($"Memory around PC:");
        for (int i = -4; i <= 4; i++)
        {
            var addr = (ushort)(pc + i);
            try
            {
                var value = _hardware.ReadMemory(addr);
                var marker = i == 0 ? " <-- PC" : "";
                Console.WriteLine($"  ${addr:X4}: ${value:X2}{marker}");
            }
            catch
            {
                Console.WriteLine($"  ${addr:X4}: ?? (read error)");
            }
        }

        Console.WriteLine("========================\n");
    }

    private void ShowFunctionInfo()
    {
        Console.WriteLine("\n=== Function Information ===");
        Console.WriteLine($"Total Functions: {_decompiler.Functions.Count}");
        Console.WriteLine($"JIT Compiled: {_jitMethods.Count}");

        var currentPC = _hardware.GetProgramCounter();
        var currentFunction = _decompiler.Functions.Values
            .FirstOrDefault(f => f.Instructions.Contains(currentPC));

        if (currentFunction != null)
        {
            Console.WriteLine($"Current Function: {currentFunction.Name} at ${currentFunction.Address:X4}");
            Console.WriteLine($"  Instructions: {currentFunction.Instructions.Count}");
            Console.WriteLine($"  Variables: {currentFunction.VariablesAccessed.Count}");
            Console.WriteLine($"  Called Functions: {currentFunction.CalledFunctions.Count}");
        }
        else
        {
            Console.WriteLine($"Current PC ${currentPC:X4} is not in a known function");
        }

        Console.WriteLine("\nTop 10 Functions by size:");
        foreach (var func in _decompiler.Functions.Values.OrderByDescending(f => f.Instructions.Count).Take(10))
        {
            var jitStatus = _jitMethods.ContainsKey(func.Address) ? "JIT" : "INT";
            Console.WriteLine($"  ${func.Address:X4}: {func.Name} ({func.Instructions.Count} instructions) [{jitStatus}]");
        }

        Console.WriteLine("===========================\n");
    }

    private void ShowHelp()
    {
        Console.WriteLine("\n=== Controls ===");
        Console.WriteLine("Game Controls:");
        Console.WriteLine("  Arrow Keys: D-Pad");
        Console.WriteLine("  Z: A Button");
        Console.WriteLine("  X: B Button");
        Console.WriteLine("  Enter: Start");
        Console.WriteLine("  Space: Select");
        Console.WriteLine();
        Console.WriteLine("Emulator Controls:");
        Console.WriteLine("  Q: Quit");
        Console.WriteLine("  S: Show detailed stats");
        Console.WriteLine("  R: Reset system");
        Console.WriteLine("  P: Pause/Resume");
        Console.WriteLine("  D: Debug information");
        Console.WriteLine("  F: Function information");
        Console.WriteLine("  H: This help");
        Console.WriteLine("===============\n");
    }

    private void ShowFinalStats()
    {
        var stats = _mainLoop.GetStats();
        var totalTime = DateTime.UtcNow - _startTime;

        Console.WriteLine("=== Final Statistics ===");
        Console.WriteLine($"Total Runtime: {totalTime:hh\\:mm\\:ss}");
        Console.WriteLine($"Total Cycles Executed: {stats.TotalCycles:N0}");
        Console.WriteLine($"Average CPS: {stats.TotalCycles / Math.Max(1, totalTime.TotalSeconds):N0}");
        Console.WriteLine($"JIT Functions Used: {_jitMethods.Count}");
        Console.WriteLine($"Functions Available: {_decompiler.Functions.Count}");
        Console.WriteLine($"JIT Coverage: {(_jitMethods.Count * 100.0 / Math.Max(1, _decompiler.Functions.Count)):F1}%");
        Console.WriteLine("========================");
    }
}