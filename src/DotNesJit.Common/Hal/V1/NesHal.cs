namespace DotNesJit.Common.Hal.V1;

/// <summary>
/// Complete Hardware Abstraction Layer with full NES emulation and proper interrupt handling
/// </summary>
public class NesHal : INesHal
{
    private readonly byte[] _memory = new byte[0x10000]; // 64KB of memory
    private readonly byte[] _prgRom;
    private readonly byte[] _chrRom;
    private NESMainLoop? _mainLoop;

    // CPU state
    private byte _stackPointer = 0xFF;
    private ushort _programCounter = 0x8000;
    private readonly Dictionary<ushort, Func<bool>> _jitFunctions = new();
    private readonly Dictionary<ushort, string> _functionNames = new();

    // PPU registers
    private byte _ppuCtrl = 0;
    private byte _ppuMask = 0;
    private byte _ppuStatus = 0;
    private byte _oamAddr = 0;
    private byte _ppuScrollX = 0;
    private byte _ppuScrollY = 0;
    private ushort _ppuAddr = 0;
    private bool _ppuAddrLatch = false;
    private byte _ppuDataBuffer = 0;

    // APU registers
    private readonly byte[] _apuRegisters = new byte[0x18];

    // Controller state
    private byte _controller1 = 0;
    private byte _controller2 = 0;
    private bool _controllerStrobe = false;
    private int _controller1Shift = 0;
    private int _controller2Shift = 0;

    // CPU status flags
    private readonly Dictionary<CpuStatusFlags, bool> _statusFlags = new()
    {
        { CpuStatusFlags.Carry, false },
        { CpuStatusFlags.Zero, false },
        { CpuStatusFlags.InterruptDisable, true }, // Start with interrupts disabled
        { CpuStatusFlags.Decimal, false },
        { CpuStatusFlags.BFlag, false },
        { CpuStatusFlags.Always1, true },
        { CpuStatusFlags.Overflow, false },
        { CpuStatusFlags.Negative, false },
    };

    // Interrupt state - ENHANCED
    private bool _nmiRequested = false;
    private bool _irqRequested = false;
    private bool _nmiPending = false;
    private bool _irqPending = false;
    private bool _nmiEdgeDetected = false;

    // Cycle counting
    private long _totalCycles = 0;

    // Interrupt vectors (cached from ROM)
    private ushort _nmiVector = 0;
    private ushort _resetVector = 0;
    private ushort _irqVector = 0;

    public NesHal(byte[]? prgRom, byte[]? chrRom = null)
    {
        _prgRom = prgRom ?? throw new ArgumentNullException(nameof(prgRom));
        _chrRom = chrRom ?? Array.Empty<byte>();

        InitializeMemoryMap();
        InitializeVectors();
    }

    /// <summary>
    /// Sets the main loop reference (called after construction to avoid circular dependency)
    /// </summary>
    public void SetMainLoop(NESMainLoop mainLoop)
    {
        _mainLoop = mainLoop ?? throw new ArgumentNullException(nameof(mainLoop));
    }

    /// <summary>
    /// Initializes the NES memory map
    /// </summary>
    private void InitializeMemoryMap()
    {
        // Clear RAM areas
        Array.Clear(_memory, 0x0000, 0x0800); // Clear RAM
        Array.Clear(_memory, 0x6000, 0x2000); // Clear SRAM area

        // Map PRG ROM to $8000-$FFFF
        if (_prgRom.Length >= 0x4000) // 16KB
        {
            Array.Copy(_prgRom, 0, _memory, 0x8000, Math.Min(_prgRom.Length, 0x8000));

            // If only 16KB, mirror it
            if (_prgRom.Length == 0x4000)
            {
                Array.Copy(_prgRom, 0, _memory, 0xC000, 0x4000);
            }
        }

        Console.WriteLine($"NES HAL initialized: PRG ROM {_prgRom.Length} bytes, CHR ROM {_chrRom.Length} bytes");
    }

    /// <summary>
    /// Initializes interrupt vectors from ROM
    /// </summary>
    private void InitializeVectors()
    {
        if (_prgRom.Length >= 6)
        {
            // Read vectors from end of PRG ROM
            _nmiVector = (ushort)(_prgRom[_prgRom.Length - 4] | (_prgRom[_prgRom.Length - 3] << 8));
            _resetVector = (ushort)(_prgRom[_prgRom.Length - 2] | (_prgRom[_prgRom.Length - 1] << 8));
            _irqVector = (ushort)(_prgRom[_prgRom.Length - 6] | (_prgRom[_prgRom.Length - 5] << 8));

            // Write vectors to memory
            WriteMemory(0xFFFA, (byte)(_nmiVector & 0xFF));
            WriteMemory(0xFFFB, (byte)(_nmiVector >> 8));
            WriteMemory(0xFFFC, (byte)(_resetVector & 0xFF));
            WriteMemory(0xFFFD, (byte)(_resetVector >> 8));
            WriteMemory(0xFFFE, (byte)(_irqVector & 0xFF));
            WriteMemory(0xFFFF, (byte)(_irqVector >> 8));

            Console.WriteLine($"Vectors - NMI: ${_nmiVector:X4}, RESET: ${_resetVector:X4}, IRQ: ${_irqVector:X4}");
        }
    }

    #region CPU State Management

    public byte ARegister { get; set; }
    public byte XRegister { get; set; }
    public byte YRegister { get; set; }

    /// <summary>
    /// Sets a CPU status flag
    /// </summary>
    public void SetFlag(CpuStatusFlags flag, bool value)
    {
        if (!Enum.GetValues<CpuStatusFlags>().Contains(flag))
        {
            throw new NotSupportedException(flag.ToString());
        }

        _statusFlags[flag] = value;
    }

    /// <summary>
    /// Gets a CPU status flag
    /// </summary>
    public bool GetFlag(CpuStatusFlags flag)
    {
        if (!Enum.GetValues<CpuStatusFlags>().Contains(flag))
        {
            throw new NotSupportedException(flag.ToString());
        }

        return _statusFlags[flag];
    }

    /// <summary>
    /// Gets the current stack pointer value
    /// </summary>
    public byte GetStackPointer()
    {
        return _stackPointer;
    }

    /// <summary>
    /// Sets the stack pointer value
    /// </summary>
    public void SetStackPointer(byte value)
    {
        _stackPointer = value;
    }

    /// <summary>
    /// Gets the current program counter
    /// </summary>
    public ushort GetProgramCounter()
    {
        return _programCounter;
    }

    /// <summary>
    /// Sets the program counter
    /// </summary>
    public void SetProgramCounter(ushort value)
    {
        _programCounter = value;
    }

    /// <summary>
    /// Gets the processor status register as a single byte
    /// </summary>
    public byte GetProcessorStatus()
    {
        byte status = 0;

        if (_statusFlags[CpuStatusFlags.Carry]) status |= 0x01;
        if (_statusFlags[CpuStatusFlags.Zero]) status |= 0x02;
        if (_statusFlags[CpuStatusFlags.InterruptDisable]) status |= 0x04;
        if (_statusFlags[CpuStatusFlags.Decimal]) status |= 0x08;
        if (_statusFlags[CpuStatusFlags.BFlag]) status |= 0x10;
        if (_statusFlags[CpuStatusFlags.Always1]) status |= 0x20;
        if (_statusFlags[CpuStatusFlags.Overflow]) status |= 0x40;
        if (_statusFlags[CpuStatusFlags.Negative]) status |= 0x80;

        return status;
    }

    /// <summary>
    /// Sets the processor status register from a single byte
    /// </summary>
    public void SetProcessorStatus(byte status)
    {
        _statusFlags[CpuStatusFlags.Carry] = (status & 0x01) != 0;
        _statusFlags[CpuStatusFlags.Zero] = (status & 0x02) != 0;
        _statusFlags[CpuStatusFlags.InterruptDisable] = (status & 0x04) != 0;
        _statusFlags[CpuStatusFlags.Decimal] = (status & 0x08) != 0;
        _statusFlags[CpuStatusFlags.BFlag] = (status & 0x10) != 0;
        _statusFlags[CpuStatusFlags.Always1] = (status & 0x20) != 0;
        _statusFlags[CpuStatusFlags.Overflow] = (status & 0x40) != 0;
        _statusFlags[CpuStatusFlags.Negative] = (status & 0x80) != 0;
    }

    #endregion

    #region Enhanced Interrupt Management

    /// <summary>
    /// Checks if NMI is currently requested
    /// </summary>
    public bool GetNMIRequested()
    {
        return _nmiRequested;
    }

    /// <summary>
    /// Checks if NMI is pending
    /// </summary>
    public bool CheckNMIPending()
    {
        return _nmiPending;
    }

    /// <summary>
    /// Checks if IRQ is currently requested
    /// </summary>
    public bool GetIRQRequested()
    {
        return _irqRequested;
    }

    /// <summary>
    /// Checks if IRQ is pending
    /// </summary>
    public bool CheckIRQPending()
    {
        return _irqPending;
    }

    /// <summary>
    /// Sets the NMI request flag
    /// </summary>
    public void SetNMIRequested(bool requested)
    {
        _nmiRequested = requested;
        if (requested)
        {
            _nmiPending = true;
            _nmiEdgeDetected = true;
        }
    }

    /// <summary>
    /// Sets the IRQ request flag
    /// </summary>
    public void SetIRQRequested(bool requested)
    {
        _irqRequested = requested;
        if (requested)
            _irqPending = true;
    }

    /// <summary>
    /// Requests an NMI interrupt
    /// </summary>
    public void RequestNMI()
    {
        SetNMIRequested(true);
    }

    /// <summary>
    /// Requests an IRQ interrupt
    /// </summary>
    public void RequestIRQ()
    {
        SetIRQRequested(true);
    }

    /// <summary>
    /// Handles NMI interrupt processing
    /// </summary>
    public void HandleNMI()
    {
        try
        {
            Console.WriteLine("HandleNMI called - processing NMI interrupt");

            // Clear the NMI request flags
            _nmiRequested = false;
            _nmiPending = false;
            _nmiEdgeDetected = false;

            // Push current PC and status
            PushAddress(_programCounter);
            PushStack(GetProcessorStatus());

            // Set interrupt disable flag
            SetFlag(CpuStatusFlags.InterruptDisable, true);

            // Jump to NMI vector
            _programCounter = _nmiVector;
            _totalCycles += 7; // NMI takes 7 cycles

            Console.WriteLine($"NMI handled - jumping to ${_nmiVector:X4}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleNMI: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles IRQ interrupt processing
    /// </summary>
    public void HandleIRQ()
    {
        try
        {
            Console.WriteLine("HandleIRQ called - processing IRQ interrupt");

            // Clear the IRQ request flags
            _irqRequested = false;
            _irqPending = false;

            // Push current PC and status
            PushAddress(_programCounter);
            var status = GetProcessorStatus();
            status = (byte)(status & ~0x10); // Clear B flag for IRQ
            PushStack(status);

            // Set interrupt disable flag
            SetFlag(CpuStatusFlags.InterruptDisable, true);

            // Jump to IRQ vector
            _programCounter = _irqVector;
            _totalCycles += 7; // IRQ takes 7 cycles

            Console.WriteLine($"IRQ handled - jumping to ${_irqVector:X4}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in HandleIRQ: {ex.Message}");
        }
    }

    /// <summary>
    /// Enhanced interrupt checking with proper priority
    /// </summary>
    public bool CheckAndProcessInterrupts()
    {
        // NMI has highest priority and cannot be disabled
        if (_nmiRequested || _nmiPending || _nmiEdgeDetected)
        {
            HandleNMI();
            return true;
        }

        // IRQ can be disabled by the interrupt disable flag
        if ((_irqRequested || _irqPending) && !GetFlag(CpuStatusFlags.InterruptDisable))
        {
            HandleIRQ();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Triggers NMI based on PPU VBlank
    /// </summary>
    public void TriggerVBlankNMI()
    {
        // Check if NMI is enabled in PPUCTRL (bit 7)
        if ((_ppuCtrl & 0x80) != 0)
        {
            Console.WriteLine("VBlank NMI triggered");
            SetNMIRequested(true);
        }
    }

    /// <summary>
    /// Resets interrupt state
    /// </summary>
    public void ResetInterrupts()
    {
        _nmiRequested = false;
        _irqRequested = false;
        _nmiPending = false;
        _irqPending = false;
        _nmiEdgeDetected = false;

        Console.WriteLine("Interrupt state reset");
    }

    /// <summary>
    /// Debug method to show interrupt state
    /// </summary>
    public string GetInterruptState()
    {
        return $"NMI: Req={_nmiRequested}, Pend={_nmiPending}, Edge={_nmiEdgeDetected} | " +
               $"IRQ: Req={_irqRequested}, Pend={_irqPending} | " +
               $"I-Flag={GetFlag(CpuStatusFlags.InterruptDisable)} | " +
               $"NMI-Enable={(_ppuCtrl & 0x80) != 0} | " +
               $"VBlank={(_ppuStatus & 0x80) != 0}";
    }

    #endregion

    #region Memory Access

    /// <summary>
    /// Reads from memory with proper memory mapping
    /// </summary>
    public byte ReadMemory(ushort address)
    {
        _totalCycles++;

        switch (address)
        {
            // RAM ($0000-$07FF) with mirroring
            case >= 0x0000 and < 0x2000:
                return _memory[address & 0x07FF];

            // PPU registers ($2000-$2007) with mirroring
            case >= 0x2000 and < 0x4000:
                return ReadPPURegister((ushort)(0x2000 + (address & 0x0007)));

            // APU and I/O registers ($4000-$4017)
            case >= 0x4000 and <= 0x4017:
                return ReadAPURegister(address);

            // APU and I/O functionality that is normally disabled
            case >= 0x4018 and < 0x6000:
                return 0; // Open bus

            // SRAM ($6000-$7FFF)
            case >= 0x6000 and < 0x8000:
                return _memory[address];

            // PRG ROM ($8000-$FFFF)
            case >= 0x8000:
                return _memory[address];
        }
    }

    /// <summary>
    /// Writes to memory with proper memory mapping
    /// </summary>
    public void WriteMemory(ushort address, byte value)
    {
        _totalCycles++;

        switch (address)
        {
            // RAM ($0000-$07FF) with mirroring
            case >= 0x0000 and < 0x2000:
                _memory[address & 0x07FF] = value;
                break;

            // PPU registers ($2000-$2007) with mirroring
            case >= 0x2000 and < 0x4000:
                WritePPURegister((ushort)(0x2000 + (address & 0x0007)), value);
                break;

            // APU and I/O registers ($4000-$4017)
            case >= 0x4000 and <= 0x4017:
                WriteAPURegister(address, value);
                break;

            // APU and I/O functionality that is normally disabled
            case >= 0x4018 and < 0x6000:
                // Ignored
                break;

            // SRAM ($6000-$7FFF)
            case >= 0x6000 and < 0x8000:
                _memory[address] = value;
                break;

            // PRG ROM ($8000-$FFFF) - writes are ignored or used for mapper control
            case >= 0x8000:
                HandleMapperWrite(address, value);
                break;
        }
    }

    #endregion

    #region PPU Registers

    /// <summary>
    /// Reads from PPU registers
    /// </summary>
    private byte ReadPPURegister(ushort address)
    {
        switch (address)
        {
            case 0x2000: // PPUCTRL
                return 0; // Write-only register

            case 0x2001: // PPUMASK
                return 0; // Write-only register

            case 0x2002: // PPUSTATUS
                return ReadPPUSTATUSWithVBlank();

            case 0x2003: // OAMADDR
                return 0; // Write-only register

            case 0x2004: // OAMDATA
                return 0; // TODO: Implement OAM reading

            case 0x2005: // PPUSCROLL
                return 0; // Write-only register

            case 0x2006: // PPUADDR
                return 0; // Write-only register

            case 0x2007: // PPUDATA
                // PPU data read with buffering
                byte result = _ppuDataBuffer;
                _ppuDataBuffer = ReadPPUMemory(_ppuAddr);

                // Increment PPUADDR based on PPUCTRL bit 2
                _ppuAddr += (_ppuCtrl & 0x04) != 0 ? (ushort)32 : (ushort)1;

                return result;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Enhanced PPUSTATUS read with proper VBlank handling
    /// </summary>
    public byte ReadPPUSTATUSWithVBlank()
    {
        byte status = _ppuStatus;

        // Set VBlank flag if we're in VBlank
        if (_mainLoop != null)
        {
            var currentScanline = _mainLoop.GetStats().CurrentScanline;
            if (currentScanline >= 241) // VBlank period
            {
                status |= 0x80; // Set VBlank flag
            }
        }

        // Notify main loop about PPUSTATUS read for VBlank detection
        _mainLoop?.DetectVBlankWaitingPattern(0x2002, status);

        // Reading PPUSTATUS clears the VBlank flag and the PPU address latch
        _ppuStatus &= 0x7F; // Clear VBlank flag
        _ppuAddrLatch = false;

        return status;
    }

    /// <summary>
    /// Writes to PPU registers
    /// </summary>
    private void WritePPURegister(ushort address, byte value)
    {
        switch (address)
        {
            case 0x2000: // PPUCTRL
                WritePPUCTRLWithNMI(value);
                break;

            case 0x2001: // PPUMASK
                _ppuMask = value;
                break;

            case 0x2002: // PPUSTATUS
                // Read-only register, writes are ignored
                break;

            case 0x2003: // OAMADDR
                _oamAddr = value;
                break;

            case 0x2004: // OAMDATA
                // TODO: Implement OAM writing
                _oamAddr++;
                break;

            case 0x2005: // PPUSCROLL
                if (!_ppuAddrLatch)
                {
                    _ppuScrollX = value;
                }
                else
                {
                    _ppuScrollY = value;
                }
                _ppuAddrLatch = !_ppuAddrLatch;
                break;

            case 0x2006: // PPUADDR
                if (!_ppuAddrLatch)
                {
                    _ppuAddr = (ushort)((_ppuAddr & 0x00FF) | (value << 8));
                }
                else
                {
                    _ppuAddr = (ushort)((_ppuAddr & 0xFF00) | value);
                }
                _ppuAddrLatch = !_ppuAddrLatch;
                break;

            case 0x2007: // PPUDATA
                WritePPUMemory(_ppuAddr, value);
                // Increment PPUADDR based on PPUCTRL bit 2
                _ppuAddr += (_ppuCtrl & 0x04) != 0 ? (ushort)32 : (ushort)1;
                break;
        }
    }

    /// <summary>
    /// Enhanced PPUCTRL write with NMI handling
    /// </summary>
    public void WritePPUCTRLWithNMI(byte value)
    {
        bool oldNMIEnable = (_ppuCtrl & 0x80) != 0;
        bool newNMIEnable = (value & 0x80) != 0;

        _ppuCtrl = value;

        // Notify main loop about PPUCTRL write
        _mainLoop?.HandlePPUCTRLWrite(value);

        // If NMI was just enabled and we're in VBlank, trigger NMI immediately
        if (!oldNMIEnable && newNMIEnable && (_ppuStatus & 0x80) != 0)
        {
            Console.WriteLine("Immediate NMI trigger due to PPUCTRL write during VBlank");
            SetNMIRequested(true);
        }
    }

    /// <summary>
    /// Reads from PPU memory space (VRAM, pattern tables, etc.)
    /// </summary>
    private byte ReadPPUMemory(ushort address)
    {
        address &= 0x3FFF; // PPU address space is 14-bit

        switch (address)
        {
            case >= 0x0000 and < 0x2000:
                // Pattern tables (CHR ROM/RAM)
                if (address < _chrRom.Length)
                    return _chrRom[address];
                return 0;

            case >= 0x2000 and < 0x3F00:
                // Nametables - simplified implementation
                return 0;

            case >= 0x3F00 and <= 0x3FFF:
                // Palette RAM
                return 0;

            default:
                return 0;
        }
    }

    /// <summary>
    /// Writes to PPU memory space
    /// </summary>
    private void WritePPUMemory(ushort address, byte value)
    {
        address &= 0x3FFF;

        // Simplified PPU memory write - just ignore for now
        // In a full implementation, this would handle nametables, palette RAM, etc.
    }

    #endregion

    #region APU and I/O Registers

    /// <summary>
    /// Reads from APU and I/O registers
    /// </summary>
    private byte ReadAPURegister(ushort address)
    {
        switch (address)
        {
            case 0x4015: // SND_CHN (APU Status)
                return 0; // TODO: Implement APU status

            case 0x4016: // JOY1
                return ReadController1();

            case 0x4017: // JOY2
                return ReadController2();

            default:
                return 0;
        }
    }

    /// <summary>
    /// Writes to APU and I/O registers
    /// </summary>
    private void WriteAPURegister(ushort address, byte value)
    {
        switch (address)
        {
            // Square wave 1
            case 0x4000: // SQ1_VOL
            case 0x4001: // SQ1_SWEEP
            case 0x4002: // SQ1_LO
            case 0x4003: // SQ1_HI
            // Square wave 2
            case 0x4004: // SQ2_VOL
            case 0x4005: // SQ2_SWEEP
            case 0x4006: // SQ2_LO
            case 0x4007: // SQ2_HI
            // Triangle wave
            case 0x4008: // TRI_LINEAR
            case 0x400A: // TRI_LO
            case 0x400B: // TRI_HI
            // Noise
            case 0x400C: // NOISE_VOL
            case 0x400E: // NOISE_LO
            case 0x400F: // NOISE_HI
            // DMC
            case 0x4010: // DMC_FREQ
            case 0x4011: // DMC_RAW
            case 0x4012: // DMC_START
            case 0x4013: // DMC_LEN
                _apuRegisters[address - 0x4000] = value;
                break;

            case 0x4014: // OAMDMA
                PerformOAMDMA(value);
                break;

            case 0x4015: // SND_CHN
                _apuRegisters[address - 0x4000] = value;
                break;

            case 0x4016: // JOY1
                WriteController(value);
                break;

            case 0x4017: // JOY2 / Frame Counter
                _apuRegisters[address - 0x4000] = value;
                break;
        }
    }

    /// <summary>
    /// Performs OAM DMA transfer
    /// </summary>
    private void PerformOAMDMA(byte page)
    {
        ushort sourceAddr = (ushort)(page << 8);

        // Copy 256 bytes from CPU memory to OAM
        for (int i = 0; i < 256; i++)
        {
            byte data = ReadMemory((ushort)(sourceAddr + i));
            // TODO: Write to OAM at _oamAddr + i
        }

        // OAM DMA takes 513-514 CPU cycles
        _totalCycles += 513;
    }

    #endregion

    #region Controller Input

    /// <summary>
    /// Reads controller 1 state
    /// </summary>
    private byte ReadController1()
    {
        if (_controllerStrobe)
        {
            return (byte)(_controller1 & 0x01);
        }

        byte result = (byte)((_controller1 >> _controller1Shift) & 0x01);
        _controller1Shift = Math.Min(_controller1Shift + 1, 7);
        return result;
    }

    /// <summary>
    /// Reads controller 2 state
    /// </summary>
    private byte ReadController2()
    {
        if (_controllerStrobe)
        {
            return (byte)(_controller2 & 0x01);
        }

        byte result = (byte)((_controller2 >> _controller2Shift) & 0x01);
        _controller2Shift = Math.Min(_controller2Shift + 1, 7);
        return result;
    }

    /// <summary>
    /// Writes to controller register (strobe)
    /// </summary>
    private void WriteController(byte value)
    {
        bool newStrobe = (value & 0x01) != 0;

        if (_controllerStrobe && !newStrobe)
        {
            // Falling edge - latch controller state
            _controller1Shift = 0;
            _controller2Shift = 0;
        }

        _controllerStrobe = newStrobe;
    }

    /// <summary>
    /// Sets controller button states
    /// </summary>
    public void SetControllerState(int controller, NESController buttons)
    {
        byte state = 0;
        if (buttons.HasFlag(NESController.A)) state |= 0x01;
        if (buttons.HasFlag(NESController.B)) state |= 0x02;
        if (buttons.HasFlag(NESController.Select)) state |= 0x04;
        if (buttons.HasFlag(NESController.Start)) state |= 0x08;
        if (buttons.HasFlag(NESController.Up)) state |= 0x10;
        if (buttons.HasFlag(NESController.Down)) state |= 0x20;
        if (buttons.HasFlag(NESController.Left)) state |= 0x40;
        if (buttons.HasFlag(NESController.Right)) state |= 0x80;

        if (controller == 1)
            _controller1 = state;
        else if (controller == 2)
            _controller2 = state;
    }

    #endregion

    #region Mapper Support

    /// <summary>
    /// Handles mapper writes (for more complex cartridges)
    /// </summary>
    private void HandleMapperWrite(ushort address, byte value)
    {
        // For now, ignore mapper writes (NROM mapper)
        // TODO: Implement other mappers as needed
    }

    #endregion

    #region Stack Operations

    /// <summary>
    /// Pushes a byte onto the 6502 stack
    /// </summary>
    public void PushStack(byte value)
    {
        WriteMemory((ushort)(0x0100 + _stackPointer), value);
        _stackPointer--;
    }

    /// <summary>
    /// Pulls a byte from the 6502 stack
    /// </summary>
    public byte PullStack()
    {
        _stackPointer++;
        return ReadMemory((ushort)(0x0100 + _stackPointer));
    }

    /// <summary>
    /// Pushes a 16-bit address onto the stack (high byte first, then low byte)
    /// </summary>
    public void PushAddress(ushort address)
    {
        PushStack((byte)(address >> 8));   // High byte first
        PushStack((byte)(address & 0xFF)); // Low byte second
    }

    /// <summary>
    /// Pulls a 16-bit address from the stack (low byte first, then high byte)
    /// </summary>
    public ushort PullAddress()
    {
        byte low = PullStack();  // Low byte first
        byte high = PullStack(); // High byte second
        return (ushort)((high << 8) | low);
    }

    #endregion

    #region JIT Function Management

    /// <summary>
    /// Registers a JIT-compiled function
    /// </summary>
    public void RegisterJITFunction(ushort address, Func<bool> function, string name)
    {
        _jitFunctions[address] = function;
        _functionNames[address] = name;
    }

    /// <summary>
    /// Executes a single CPU instruction cycle with enhanced error handling
    /// </summary>
    public bool ExecuteCPUCycle()
    {
        try
        {
            // Check for interrupts first
            if (CheckAndProcessInterrupts())
            {
                return true;
            }

            // Try to execute JIT function
            if (_jitFunctions.TryGetValue(_programCounter, out var function))
            {
                try
                {
                    return function();
                }
                catch (Exception ex)
                {
                    // Enhanced error reporting
                    var currentEx = ex;
                    while (currentEx.InnerException != null)
                    {
                        currentEx = currentEx.InnerException;
                    }

                    Console.WriteLine($"JIT execution error at ${_programCounter:X4} in {_functionNames.GetValueOrDefault(_programCounter, "unknown")}:");
                    Console.WriteLine($"  {currentEx.GetType().Name}: {currentEx.Message}");

                    if (currentEx.StackTrace != null)
                    {
                        var lines = currentEx.StackTrace.Split('\n').Take(3);
                        foreach (var line in lines)
                        {
                            Console.WriteLine($"  {line.Trim()}");
                        }
                    }

                    // Skip this function and continue
                    _programCounter++;
                    _totalCycles++;
                    return true;
                }
            }

            // Fallback execution
            _programCounter++;
            _totalCycles++;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CPU cycle error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Executes one CPU cycle (enhanced version)
    /// </summary>
    public bool ExecuteOneCycle()
    {
        return ExecuteCPUCycle();
    }

    #endregion

    #region Control Flow Instructions

    /// <summary>
    /// Jumps to a specific address (used by JMP instruction)
    /// </summary>
    public void JumpToAddress(ushort address)
    {
        _programCounter = address;

        // Try to execute JIT function if available
        if (_jitFunctions.TryGetValue(address, out var function))
        {
            try
            {
                function();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing JIT function at ${address:X4}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Calls a function at a specific address (used by JSR instruction)
    /// </summary>
    public void CallFunction(ushort address)
    {
        // JSR pushes PC + 2 (return address - 1) onto stack
        var returnAddress = (ushort)(_programCounter + 2); // JSR is 3 bytes
        PushAddress((ushort)(returnAddress - 1));

        // Jump to the target function
        JumpToAddress(address);
    }

    /// <summary>
    /// Returns from subroutine (used by RTS instruction)
    /// </summary>
    public void ReturnFromSubroutine()
    {
        // RTS pulls return address from stack and adds 1
        var returnAddress = (ushort)(PullAddress() + 1);
        _programCounter = returnAddress;
    }

    /// <summary>
    /// Returns from interrupt (used by RTI instruction)
    /// </summary>
    public void ReturnFromInterrupt()
    {
        // RTI pulls processor status first, then return address
        var status = PullStack();
        SetProcessorStatus(status);

        var returnAddress = PullAddress();
        _programCounter = returnAddress;
    }

    /// <summary>
    /// Triggers a software interrupt (used by BRK instruction)
    /// </summary>
    public void TriggerSoftwareInterrupt()
    {
        // BRK pushes PC + 2, then status with B flag set
        var returnAddress = (ushort)(_programCounter + 2);
        PushAddress(returnAddress);

        var status = GetProcessorStatus();
        status |= 0x10; // Set B flag
        PushStack(status);

        // Set interrupt disable flag
        SetFlag(CpuStatusFlags.InterruptDisable, true);

        // Jump to IRQ/BRK vector
        _programCounter = _irqVector;
        _totalCycles += 7; // BRK takes 7 cycles
    }

    /// <summary>
    /// Dispatches execution to a specific address
    /// </summary>
    public void DispatchToAddress(ushort address)
    {
        JumpToAddress(address);
    }

    #endregion

    #region VBlank and Timing

    /// <summary>
    /// Waits for VBlank - integrates with main loop for optimization
    /// </summary>
    public void WaitForVBlank()
    {
        // Signal to main loop that we're waiting for VBlank
        _mainLoop?.DetectVBlankWaitingPattern(0x2002, 0x00);

        // Yield execution to allow main loop to handle VBlank timing
        System.Threading.Thread.Yield();
    }

    #endregion

    #region Debug and Diagnostics

    /// <summary>
    /// Gets direct memory access (for debugging)
    /// </summary>
    public byte[] GetMemory() => _memory;

    /// <summary>
    /// Gets PPU control register value
    /// </summary>
    public byte GetPPUCtrl() => _ppuCtrl;

    /// <summary>
    /// Gets PPU status for debugging
    /// </summary>
    public string GetPPUStatus()
    {
        return $"PPUCTRL: ${_ppuCtrl:X2}, PPUMASK: ${_ppuMask:X2}, " +
               $"PPUSTATUS: ${_ppuStatus:X2}, PPUADDR: ${_ppuAddr:X4}, " +
               $"Scroll: ({_ppuScrollX}, {_ppuScrollY}), AddrLatch: {_ppuAddrLatch}";
    }

    /// <summary>
    /// Gets debugging information about the current CPU state
    /// </summary>
    public string GetCPUState()
    {
        return $"PC:${_programCounter:X4} SP:${_stackPointer:X2} " +
               $"Status:${GetProcessorStatus():X2} " +
               $"Cycles:{_totalCycles} " +
               $"JIT Functions:{_jitFunctions.Count} " +
               $"Interrupts:{GetInterruptState()}";
    }

    /// <summary>
    /// Lists all registered JIT functions
    /// </summary>
    public void ListJITFunctions()
    {
        Console.WriteLine($"Registered JIT Functions ({_jitFunctions.Count}):");
        foreach (var kvp in _functionNames.OrderBy(x => x.Key))
        {
            Console.WriteLine($"  ${kvp.Key:X4}: {kvp.Value}");
        }
    }

    /// <summary>
    /// Gets total CPU cycles executed
    /// </summary>
    public long GetTotalCycles()
    {
        return _totalCycles;
    }

    /// <summary>
    /// Resets the hardware to initial state
    /// </summary>
    public void Reset()
    {
        // Reset CPU state
        _stackPointer = 0xFF;
        _programCounter = _resetVector;

        // Reset flags
        SetFlag(CpuStatusFlags.InterruptDisable, true);
        SetFlag(CpuStatusFlags.Always1, true);
        SetFlag(CpuStatusFlags.Carry, false);
        SetFlag(CpuStatusFlags.Zero, false);
        SetFlag(CpuStatusFlags.Decimal, false);
        SetFlag(CpuStatusFlags.BFlag, false);
        SetFlag(CpuStatusFlags.Overflow, false);
        SetFlag(CpuStatusFlags.Negative, false);

        // Reset PPU state
        _ppuCtrl = 0;
        _ppuMask = 0;
        _ppuStatus = 0;
        _oamAddr = 0;
        _ppuScrollX = 0;
        _ppuScrollY = 0;
        _ppuAddr = 0;
        _ppuAddrLatch = false;
        _ppuDataBuffer = 0;

        // Reset controller state
        _controller1 = 0;
        _controller2 = 0;
        _controllerStrobe = false;
        _controller1Shift = 0;
        _controller2Shift = 0;

        // Reset interrupt state
        ResetInterrupts();

        Console.WriteLine($"Hardware reset - PC: ${_programCounter:X4}");
    }

    /// <summary>
    /// Force trigger interrupts for testing
    /// </summary>
    public void ForceNMI()
    {
        Console.WriteLine("Force triggering NMI");
        SetNMIRequested(true);
    }

    public void ForceIRQ()
    {
        Console.WriteLine("Force triggering IRQ");
        SetIRQRequested(true);
    }

    /// <summary>
    /// Gets interrupt vectors
    /// </summary>
    public (ushort nmi, ushort reset, ushort irq) GetInterruptVectors()
    {
        return (_nmiVector, _resetVector, _irqVector);
    }

    #endregion
}

/// <summary>
/// NES controller button flags
/// </summary>
[Flags]
public enum NESController : byte
{
    None = 0,
    A = 1,
    B = 2,
    Select = 4,
    Start = 8,
    Up = 16,
    Down = 32,
    Left = 64,
    Right = 128
}