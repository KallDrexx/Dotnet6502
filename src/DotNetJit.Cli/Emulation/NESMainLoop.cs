using System;
using System.Threading;
using System.Collections.Generic;

namespace DotNetJit.Cli.Emulation
{
    /// <summary>
    /// Enhanced NES main loop implementation with multiple execution strategies
    /// Integrates with JIT compilation for optimal performance
    /// </summary>
    public class NESMainLoop
    {
        // NES timing constants (NTSC)
        private const int CPU_CYCLES_PER_FRAME = 29780;
        private const int PPU_CYCLES_PER_CPU_CYCLE = 3;
        private const int SCANLINES_PER_FRAME = 262;
        private const int CYCLES_PER_SCANLINE = 341;
        private const int VISIBLE_SCANLINES = 240;
        private const int VBLANK_SCANLINE = 241;
        private const int FRAMES_PER_SECOND = 60;

        // System state
        private bool running = false;
        private bool vblankFlag = false;
        private bool nmiEnabled = false;
        private int currentScanline = 0;
        private int scanlineCycle = 0;
        private long totalCycles = 0;
        private readonly NesHal hardware;

        // JIT-compiled function delegates
        public delegate void CPUStepDelegate();
        public delegate void NMIHandlerDelegate();
        public delegate void IRQHandlerDelegate();
        public delegate void FrameCompleteDelegate();
        public delegate bool VBlankWaitDetectionDelegate();

        // Function pointers for JIT-compiled methods
        public CPUStepDelegate CPUStep { get; set; }
        public NMIHandlerDelegate NMIHandler { get; set; }
        public IRQHandlerDelegate IRQHandler { get; set; }
        public FrameCompleteDelegate FrameComplete { get; set; }
        public VBlankWaitDetectionDelegate VBlankWaitDetection { get; set; }

        // VBlank waiting pattern detection
        private readonly Queue<ushort> recentMemoryReads = new Queue<ushort>();
        private const int VBLANK_DETECTION_WINDOW = 10;
        private int consecutiveVBlankReads = 0;

        // Performance tracking
        private DateTime lastFpsCheck = DateTime.UtcNow;
        private int frameCount = 0;
        private double currentFps = 0;

        public NESMainLoop(NesHal hardware)
        {
            this.hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
        }

        /// <summary>
        /// Cycle-accurate main loop - most accurate but slower
        /// </summary>
        public void RunCycleAccurate()
        {
            running = true;
            var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / FRAMES_PER_SECOND);

            Console.WriteLine("Starting cycle-accurate emulation...");

            while (running)
            {
                var frameStart = DateTime.UtcNow;

                // Execute one frame worth of cycles
                for (int cycles = 0; cycles < CPU_CYCLES_PER_FRAME && running; cycles++)
                {
                    // Execute one CPU instruction
                    CPUStep?.Invoke();

                    // Update PPU (3 PPU cycles per CPU cycle)
                    for (int ppuCycle = 0; ppuCycle < PPU_CYCLES_PER_CPU_CYCLE; ppuCycle++)
                    {
                        UpdatePPU();
                    }

                    totalCycles++;

                    // Check for interrupts
                    CheckInterrupts();
                }

                // Frame is complete
                FrameComplete?.Invoke();
                UpdateFPS();

                // Timing - sleep to maintain 60 FPS
                var frameTime = DateTime.UtcNow - frameStart;
                var sleepTime = targetFrameTime - frameTime;
                if (sleepTime > TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        /// <summary>
        /// Instruction-based main loop - simpler but less accurate
        /// </summary>
        public void RunInstructionBased()
        {
            running = true;
            int instructionsPerFrame = 1000; // Rough estimate, adjust as needed
            var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / FRAMES_PER_SECOND);

            Console.WriteLine("Starting instruction-based emulation...");

            while (running)
            {
                var frameStart = DateTime.UtcNow;

                // Execute instructions for one frame
                for (int i = 0; i < instructionsPerFrame && running; i++)
                {
                    CPUStep?.Invoke();

                    // Simple scanline simulation
                    if (i % (instructionsPerFrame / SCANLINES_PER_FRAME) == 0)
                    {
                        UpdateScanline();
                    }

                    // Check for interrupts occasionally
                    if (i % 10 == 0)
                    {
                        CheckInterrupts();
                    }
                }

                // Frame complete
                FrameComplete?.Invoke();
                UpdateFPS();

                // Timing
                var frameTime = DateTime.UtcNow - frameStart;
                var sleepTime = targetFrameTime - frameTime;
                if (sleepTime > TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        /// <summary>
        /// Event-driven main loop - most game-like approach
        /// This is the recommended approach for most games
        /// </summary>
        public void RunEventDriven()
        {
            running = true;
            bool waitingForVBlank = false;
            var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / FRAMES_PER_SECOND);

            Console.WriteLine("Starting event-driven emulation...");

            while (running)
            {
                var frameStart = DateTime.UtcNow;

                if (!waitingForVBlank)
                {
                    // Execute CPU instructions until we detect VBlank waiting
                    int instructionCount = 0;
                    while (!waitingForVBlank && running && instructionCount < 10000) // Safety limit
                    {
                        CPUStep?.Invoke();
                        instructionCount++;

                        // Check if game is waiting for VBlank
                        waitingForVBlank = DetectVBlankWaiting();

                        // Check for interrupts occasionally
                        if (instructionCount % 50 == 0)
                        {
                            CheckInterrupts();
                        }
                    }
                }
                else
                {
                    // Simulate waiting for VBlank
                    var frameTime = DateTime.UtcNow - frameStart;
                    var remainingTime = targetFrameTime - frameTime;

                    if (remainingTime > TimeSpan.Zero)
                    {
                        Thread.Sleep((int)Math.Min(remainingTime.TotalMilliseconds, 16));
                    }

                    // Trigger VBlank
                    TriggerVBlank();
                    waitingForVBlank = false;

                    // Frame complete
                    FrameComplete?.Invoke();
                    UpdateFPS();
                }
            }
        }

        /// <summary>
        /// Hybrid approach combining accuracy with performance
        /// Switches between modes based on game behavior
        /// </summary>
        public void RunHybrid()
        {
            running = true;
            var targetFrameTime = TimeSpan.FromMilliseconds(1000.0 / FRAMES_PER_SECOND);
            bool useEventDriven = true;
            int framesSinceSwitch = 0;

            Console.WriteLine("Starting hybrid emulation...");

            while (running)
            {
                var frameStart = DateTime.UtcNow;
                framesSinceSwitch++;

                // Decide which mode to use
                if (framesSinceSwitch > 60) // Re-evaluate every second
                {
                    useEventDriven = ShouldUseEventDriven();
                    framesSinceSwitch = 0;
                    Console.WriteLine($"Switching to {(useEventDriven ? "event-driven" : "cycle-accurate")} mode");
                }

                if (useEventDriven)
                {
                    RunEventDrivenFrame();
                }
                else
                {
                    RunCycleAccurateFrame();
                }

                // Frame complete
                FrameComplete?.Invoke();
                UpdateFPS();

                // Timing
                var frameTime = DateTime.UtcNow - frameStart;
                var sleepTime = targetFrameTime - frameTime;
                if (sleepTime > TimeSpan.Zero)
                {
                    Thread.Sleep(sleepTime);
                }
            }
        }

        /// <summary>
        /// Updates PPU state for one cycle
        /// </summary>
        private void UpdatePPU()
        {
            scanlineCycle++;

            if (scanlineCycle >= CYCLES_PER_SCANLINE)
            {
                scanlineCycle = 0;
                currentScanline++;

                if (currentScanline > SCANLINES_PER_FRAME)
                {
                    currentScanline = 0;
                }

                UpdateScanline();
            }
        }

        /// <summary>
        /// Updates scanline-based events
        /// </summary>
        private void UpdateScanline()
        {
            if (currentScanline == VBLANK_SCANLINE)
            {
                TriggerVBlank();
            }
            else if (currentScanline == 0)
            {
                // End of VBlank
                vblankFlag = false;
            }
        }

        /// <summary>
        /// Triggers VBlank and potentially NMI
        /// </summary>
        private void TriggerVBlank()
        {
            vblankFlag = true;

            // Check if NMI should be triggered
            if (nmiEnabled)
            {
                NMIHandler?.Invoke();
            }
        }

        /// <summary>
        /// Detects if the game is waiting for VBlank
        /// This uses pattern recognition to identify common VBlank waiting loops
        /// </summary>
        private bool DetectVBlankWaiting()
        {
            // Use custom detection delegate if provided
            if (VBlankWaitDetection != null)
            {
                return VBlankWaitDetection();
            }

            // Default detection: look for repeated PPUSTATUS reads
            return consecutiveVBlankReads >= 3;
        }

        /// <summary>
        /// Enhanced VBlank waiting pattern detection
        /// Recognizes common patterns like:
        /// - LDA $2002 / BPL loop (wait for bit 7 set)
        /// - LDA $2002 / AND #$80 / BEQ loop
        /// </summary>
        public bool DetectVBlankWaitingPattern(ushort lastReadAddress, byte lastReadValue)
        {
            // Track recent memory reads
            recentMemoryReads.Enqueue(lastReadAddress);
            if (recentMemoryReads.Count > VBLANK_DETECTION_WINDOW)
            {
                recentMemoryReads.Dequeue();
            }

            // Count consecutive PPUSTATUS ($2002) reads
            if (lastReadAddress == 0x2002)
            {
                consecutiveVBlankReads++;
            }
            else
            {
                consecutiveVBlankReads = 0;
            }

            // If we see multiple consecutive PPUSTATUS reads, likely waiting for VBlank
            if (consecutiveVBlankReads >= 3)
            {
                return true;
            }

            // Check for pattern: multiple $2002 reads in recent history
            int ppuStatusReads = 0;
            foreach (var addr in recentMemoryReads)
            {
                if (addr == 0x2002) ppuStatusReads++;
            }

            // If more than half of recent reads are PPUSTATUS, likely waiting
            return ppuStatusReads > VBLANK_DETECTION_WINDOW / 2;
        }

        /// <summary>
        /// Checks for pending interrupts
        /// </summary>
        private void CheckInterrupts()
        {
            // Check for IRQ if not disabled
            if (!hardware.GetFlag(CpuStatusFlags.InterruptDisable))
            {
                // IRQ logic would go here
                // IRQHandler?.Invoke();
            }

            // NMI is handled by VBlank trigger
        }

        /// <summary>
        /// Determines if event-driven mode should be used
        /// </summary>
        private bool ShouldUseEventDriven()
        {
            // Heuristic: if we've detected VBlank waiting patterns recently, use event-driven
            return consecutiveVBlankReads > 0;
        }

        /// <summary>
        /// Runs one frame in event-driven mode
        /// </summary>
        private void RunEventDrivenFrame()
        {
            bool waitingForVBlank = false;
            int instructionCount = 0;

            while (!waitingForVBlank && instructionCount < 10000)
            {
                CPUStep?.Invoke();
                instructionCount++;
                waitingForVBlank = DetectVBlankWaiting();

                if (instructionCount % 50 == 0)
                {
                    CheckInterrupts();
                }
            }

            if (waitingForVBlank)
            {
                TriggerVBlank();
            }
        }

        /// <summary>
        /// Runs one frame in cycle-accurate mode
        /// </summary>
        private void RunCycleAccurateFrame()
        {
            for (int cycles = 0; cycles < CPU_CYCLES_PER_FRAME; cycles++)
            {
                CPUStep?.Invoke();

                for (int ppuCycle = 0; ppuCycle < PPU_CYCLES_PER_CPU_CYCLE; ppuCycle++)
                {
                    UpdatePPU();
                }

                totalCycles++;

                if (cycles % 100 == 0)
                {
                    CheckInterrupts();
                }
            }
        }

        /// <summary>
        /// Updates FPS counter
        /// </summary>
        private void UpdateFPS()
        {
            frameCount++;
            var elapsed = DateTime.UtcNow - lastFpsCheck;

            if (elapsed.TotalSeconds >= 1.0)
            {
                currentFps = frameCount / elapsed.TotalSeconds;
                frameCount = 0;
                lastFpsCheck = DateTime.UtcNow;

                if (frameCount % 60 == 0) // Log every second
                {
                    Console.WriteLine($"FPS: {currentFps:F1}, Cycles: {totalCycles}, VBlank reads: {consecutiveVBlankReads}");
                }
            }
        }

        /// <summary>
        /// Handles PPUCTRL writes (NMI enable/disable)
        /// Call this from your memory write handler when address $2000 is written
        /// </summary>
        public void HandlePPUCTRLWrite(byte value)
        {
            // Bit 7 controls NMI enable
            bool newNmiEnabled = (value & 0x80) != 0;

            // If NMI was just enabled and VBlank flag is set, trigger NMI immediately
            if (!nmiEnabled && newNmiEnabled && vblankFlag)
            {
                NMIHandler?.Invoke();
            }

            nmiEnabled = newNmiEnabled;
        }

        /// <summary>
        /// Handles PPUSTATUS reads
        /// Call this from your memory read handler when address $2002 is read
        /// </summary>
        public byte HandlePPUSTATUSRead()
        {
            byte status = 0;

            // Bit 7 = VBlank flag
            if (vblankFlag)
                status |= 0x80;

            // Bit 6 = Sprite 0 hit (not implemented)
            // Bit 5 = Sprite overflow (not implemented)

            // Reading PPUSTATUS clears the VBlank flag
            vblankFlag = false;

            return status;
        }

        /// <summary>
        /// Stop the main loop
        /// </summary>
        public void Stop()
        {
            running = false;
            Console.WriteLine("NES emulation stopped.");
        }

        /// <summary>
        /// Reset the system state
        /// </summary>
        public void Reset()
        {
            currentScanline = 0;
            scanlineCycle = 0;
            vblankFlag = false;
            nmiEnabled = false;
            totalCycles = 0;
            consecutiveVBlankReads = 0;
            recentMemoryReads.Clear();
            frameCount = 0;
            lastFpsCheck = DateTime.UtcNow;

            Console.WriteLine("NES system reset.");
        }

        /// <summary>
        /// Get current system state for debugging
        /// </summary>
        public string GetSystemState()
        {
            return $"Scanline: {currentScanline}, Cycle: {scanlineCycle}, " +
                   $"VBlank: {vblankFlag}, NMI Enabled: {nmiEnabled}, " +
                   $"Total Cycles: {totalCycles}, " +
                   $"VBlank Reads: {consecutiveVBlankReads}, " +
                   $"FPS: {currentFps:F1}";
        }

        /// <summary>
        /// Gets performance statistics
        /// </summary>
        public EmulationStats GetStats()
        {
            return new EmulationStats
            {
                TotalCycles = totalCycles,
                CurrentScanline = currentScanline,
                VBlankDetections = consecutiveVBlankReads,
                IsRunning = running,
                CurrentFPS = currentFps
            };
        }
    }

    /// <summary>
    /// Performance and state statistics for the emulation
    /// </summary>
    public class EmulationStats
    {
        public long TotalCycles { get; set; }
        public int CurrentScanline { get; set; }
        public int VBlankDetections { get; set; }
        public bool IsRunning { get; set; }
        public double CurrentFPS { get; set; }
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public TimeSpan RunTime => DateTime.UtcNow - StartTime;
        public double CyclesPerSecond => TotalCycles / Math.Max(1, RunTime.TotalSeconds);

        public override string ToString()
        {
            return $"Runtime: {RunTime:mm\\:ss}, Cycles: {TotalCycles:N0}, " +
                   $"CPS: {CyclesPerSecond:N0}, FPS: {CurrentFPS:F1}, " +
                   $"Scanline: {CurrentScanline}, VBlank Reads: {VBlankDetections}";
        }
    }
}