using System.Reflection;
using DotNesJit.Common;
using DotNesJit.Common.Hal.V1;
using NESDecompiler.Core.Decompilation;
using NESDecompiler.Core.ROM;

/// <summary>
/// Advanced NES emulator with full JIT integration and enhanced error handling
/// TEMPORARY VERSION: Some advanced features disabled due to JIT compilation issues
/// This preserves all original functionality while fixing the immediate compilation problems
/// </summary>
public class AdvancedNESEmulator
{
    private readonly ROMInfo _romInfo;
    private readonly byte[] _prgRom;
    private readonly byte[] _chrRom;
    private readonly Decompiler _decompiler;
    private readonly Assembly? _jitAssembly;
    private NesHal? _hardware;
    private NESMainLoop? _mainLoop;
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

            // TEMPORARY: Enhanced logging for debugging the 0 cycles issue
            if (verbose)
            {
                Console.WriteLine($"[MAGNIFYING_GLASS] DEBUG INFO:");
                Console.WriteLine($"  Hardware initialized: {_hardware != null}");
                Console.WriteLine($"  JIT Assembly loaded: {_jitAssembly != null}");
                Console.WriteLine($"  JIT Methods mapped: {_jitMethods.Count}");
                Console.WriteLine($"  Reset Vector: ${_romInfo.ResetVector:X4}");
                Console.WriteLine($"  Current PC: ${_hardware?.GetProgramCounter():X4}");
            }

            // Run emulation in background thread
            var emulationThread = new System.Threading.Thread(() => RunEmulationLoop(mode, verbose))
            {
                IsBackground = true,
                Name = "NES Emulation"
            };

            emulationThread.Start();

            // Handle user input on main thread
            HandleUserInput();

            // Clean shutdown
            _mainLoop!.Stop();
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
            SetupJitFunctions();
        }
        else
        {
            Console.WriteLine("[WARNING] Warning: No JIT assembly - running in interpretation mode");
            Console.WriteLine("   This may cause 0 cycles issue if interpretation is not implemented");
        }

        // Configure main loop delegates
        _mainLoop.CPUStep = ExecuteCpuStep;
        _mainLoop.NMIHandler = ExecuteNmiHandler;
        _mainLoop.FrameComplete = OnFrameComplete;
        _mainLoop.VBlankWaitDetection = DetectVBlankWaiting;

        Console.WriteLine($"Emulation setup complete:");
        Console.WriteLine($"  Reset Vector: ${_romInfo.ResetVector:X4}");
        Console.WriteLine($"  JIT Functions: {_jitMethods.Count}");
        Console.WriteLine($"  Mode: Advanced with full hardware emulation");

        // TEMPORARY: Additional debug info for troubleshooting 0 cycles issue
        if (_jitMethods.Count == 0)
        {
            Console.WriteLine($"  [WARNING] WARNING: 0 JIT functions registered!");
            Console.WriteLine($"     This will likely cause the 0 cycles issue");
            Console.WriteLine($"     CPU execution will fall back to interpretation");
            Console.WriteLine($"     Check JIT compilation process above for errors");
        }
        else
        {
            Console.WriteLine($"  [CHECKMARK] JIT functions registered - cycles should increment properly");
        }
    }

    private void SetupJitFunctions()
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
                        _hardware!.RegisterJITFunction(function.Address, () =>
                        {
                            try
                            {
                                method.Invoke(null, null);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                // TEMPORARY: Enhanced error reporting for debugging
                                var realException = ex;
                                while (realException.InnerException != null)
                                    realException = realException.InnerException;

                                Console.WriteLine($"[ERROR] JIT ERROR in {function.Name} at ${function.Address:X4}:");
                                Console.WriteLine($"   Exception: {realException.GetType().FullName}");
                                Console.WriteLine($"   Message: {realException.Message}");

                                // Only show stack trace in verbose mode to avoid spam
                                if (Console.Out == Console.Error) // Simple way to detect verbose
                                {
                                    if (realException.StackTrace != null)
                                    {
                                        var lines = realException.StackTrace.Split('\n').Take(3);
                                        foreach (var line in lines)
                                        {
                                            Console.WriteLine($"   {line.Trim()}");
                                        }
                                    }
                                }

                                return false;
                            }
                        }, function.Name);
                    }
                }

                Console.WriteLine($"  [CHECKMARK] Mapped {_jitMethods.Count} JIT functions");
            }
            else
            {
                Console.WriteLine("  [WARNING] Could not find Game class in JIT assembly");
                var types = _jitAssembly.GetTypes();
                Console.WriteLine($"  Available types: {string.Join(", ", types.Select(t => t.Name))}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [WARNING] JIT setup error: {ex.Message}");
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
                    _mainLoop!.RunEventDriven();
                    break;
                case "cycle-accurate":
                    _mainLoop!.RunCycleAccurate();
                    break;
                case "instruction-based":
                    _mainLoop!.RunInstructionBased();
                    break;
                case "hybrid":
                    _mainLoop!.RunHybrid();
                    break;
                default:
                    Console.WriteLine("Unknown mode, using event-driven...");
                    _mainLoop!.RunEventDriven();
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

    private void ExecuteCpuStep()
    {
        if (_isPaused)
        {
            Thread.Sleep(100);
            return;
        }

        // TEMPORARY: Enhanced debugging for 0 cycles issue
        // Execute one CPU cycle using JIT or interpretation
        bool success = _hardware!.ExecuteCPUCycle();

        if (!success)
        {
            // TEMPORARY: Log when CPU execution fails
            Console.WriteLine($"[WARNING] CPU execution failed at PC: ${_hardware.GetProgramCounter():X4}");
        }
    }

    private void ExecuteNmiHandler()
    {
        // Try to find and execute NMI handler function
        var vectors = _hardware!.GetInterruptVectors();

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
            Thread.Sleep(1);
        }
    }

    private bool DetectVBlankWaiting()
    {
        // Only detect VBlank waiting if we see a very specific pattern
        var pc = _hardware!.GetProgramCounter();

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

    private void HandleUserInput()
    {
        Console.WriteLine("Emulation started. Press any key for controls...");

        while (_mainLoop!.GetStats().IsRunning)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                // Reset controller state
                var controller = NESController.None;

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
                _hardware!.SetControllerState(1, controller);
            }

            Thread.Sleep(50);
        }
    }

    private void ResetSystem()
    {
        _mainLoop!.Reset();
        _hardware!.Reset();
        Console.WriteLine($"System reset - PC: ${_romInfo.ResetVector:X4}");
    }

    private void ShowDetailedStats()
    {
        var stats = _mainLoop!.GetStats();
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
        Console.WriteLine($"CPU State: {_hardware!.GetCPUState()}");
        Console.WriteLine($"PPU State: {_hardware.GetPPUStatus()}");
        Console.WriteLine($"Interrupt State: {_hardware.GetInterruptState()}");

        // TEMPORARY: Additional debugging info
        if (stats.TotalCycles == 0)
        {
            Console.WriteLine("[ERROR] PROBLEM: Total cycles is 0 - CPU not executing!");
            Console.WriteLine($"   PC stuck at: ${_hardware.GetProgramCounter():X4}");
            Console.WriteLine($"   JIT Functions available: {_jitMethods.Count}");
            if (_jitMethods.Count == 0)
            {
                Console.WriteLine("   -> Issue: No JIT functions registered");
            }
            else
            {
                Console.WriteLine("   -> Issue: JIT functions not being called or failing");
            }
        }

        Console.WriteLine("=====================================\n");
    }

    private void ShowDebugInfo()
    {
        var pc = _hardware!.GetProgramCounter();
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

        // TEMPORARY: JIT function debugging
        Console.WriteLine($"JIT Functions at current PC:");
        if (_jitMethods.ContainsKey(pc))
        {
            Console.WriteLine($"  [CHECKMARK] JIT function available at ${pc:X4}");
        }
        else
        {
            Console.WriteLine($"  [ERROR] No JIT function at ${pc:X4}");
            Console.WriteLine($"  Available JIT addresses: {string.Join(", ", _jitMethods.Keys.Select(k => $"${k:X4}"))}");
        }

        Console.WriteLine("========================\n");
    }

    private void ShowFunctionInfo()
    {
        Console.WriteLine("\n=== Function Information ===");
        Console.WriteLine($"Total Functions: {_decompiler.Functions.Count}");
        Console.WriteLine($"JIT Compiled: {_jitMethods.Count}");

        var currentPc = _hardware!.GetProgramCounter();
        var currentFunction = _decompiler.Functions.Values
            .FirstOrDefault(f => f.Instructions.Contains(currentPc));

        if (currentFunction != null)
        {
            Console.WriteLine($"Current Function: {currentFunction.Name} at ${currentFunction.Address:X4}");
            Console.WriteLine($"  Instructions: {currentFunction.Instructions.Count}");
            Console.WriteLine($"  Variables: {currentFunction.VariablesAccessed.Count}");
            Console.WriteLine($"  Called Functions: {currentFunction.CalledFunctions.Count}");
        }
        else
        {
            Console.WriteLine($"Current PC ${currentPc:X4} is not in a known function");
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
        var stats = _mainLoop!.GetStats();
        var totalTime = DateTime.UtcNow - _startTime;

        Console.WriteLine("=== Final Statistics ===");
        Console.WriteLine($"Total Runtime: {totalTime:hh\\:mm\\:ss}");
        Console.WriteLine($"Total Cycles Executed: {stats.TotalCycles:N0}");
        Console.WriteLine($"Average CPS: {stats.TotalCycles / Math.Max(1, totalTime.TotalSeconds):N0}");
        Console.WriteLine($"JIT Functions Used: {_jitMethods.Count}");
        Console.WriteLine($"Functions Available: {_decompiler.Functions.Count}");
        Console.WriteLine($"JIT Coverage: {(_jitMethods.Count * 100.0 / Math.Max(1, _decompiler.Functions.Count)):F1}%");

        // TEMPORARY: Success/failure analysis
        if (stats.TotalCycles > 0)
        {
            Console.WriteLine("[CHECKMARK] SUCCESS: CPU executed instructions (cycles > 0)");
            Console.WriteLine("   The basic JIT compilation and execution is working!");
        }
        else
        {
            Console.WriteLine("[ERROR] FAILURE: CPU did not execute (cycles = 0)");
            Console.WriteLine("   Check JIT function registration and ExecuteCPUCycle implementation");
        }

        Console.WriteLine("========================");
    }
}