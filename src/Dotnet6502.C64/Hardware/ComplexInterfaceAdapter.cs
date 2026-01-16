using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// The CIA chip that the c64 uses for I/O control (MOS 6526 chip)
/// </summary>
public class ComplexInterfaceAdapter : IMemoryDevice
{
    // CRA/CRB bit masks
    private const byte TimerStart = 0b0000_0001;      // Bit 0: Start timer
    private const byte TimerRunMode = 0b0000_0010;    // Bit 1: 0=continuous, 1=one-shot
    private const byte TimerPbOutput = 0b0000_0100;   // Bit 2: Enable output to PB6/PB7
    private const byte TimerPbToggle = 0b0000_1000;   // Bit 3: 0=pulse, 1=toggle
    private const byte ForceLoad = 0b0001_0000;       // Bit 4: Force load latch into timer
    private const byte TimerInputMode = 0b0010_0000;  // Bit 5: 0=Phi2, 1=CNT pin
    private const byte SerialPortMode = 0b0100_0000;  // Bit 6 (CRA): 0=input, 1=output
    private const byte TodFrequencySelect = 0b1000_0000; // Bit 7 (CRA): 0=60Hz, 1=50Hz

    // CRB-specific (bits 5-6 select Timer B input source)
    // 00 = Phi2 clock
    // 01 = CNT positive edge
    // 10 = Timer A underflow
    // 11 = Timer A underflow while CNT high
    private const byte TimerBCountModeMask = 0b0110_0000;
    private const byte TodAlarmSetMode = 0b1000_0000; // Bit 7 (CRB): 0=write TOD, 1=write Alarm

    // ICR bit masks
    private const byte IcrTimerA = 0x01;
    private const byte IcrTimerB = 0x02;
    private const byte IcrTodAlarm = 0x04;
    private const byte IcrSerialComplete = 0x08;
    private const byte IcrFlagPin = 0x10;

    // TOD cycle timing (NTSC ~985248 Hz CPU clock)
    private const int CyclesPerTodTick60Hz = 16421;  // ~985248 / 60
    private const int CyclesPerTodTick50Hz = 19705;  // ~985248 / 50

    private readonly Timer _timerA = new();
    private readonly Timer _timerB = new();

    private byte _interruptFlags;  // Latched interrupt sources (bits 0-4)
    private byte _interruptMask;   // Which sources can trigger IRQ

    // TOD state
    private int _todCycleCounter;
    private byte _todTenths, _todSeconds, _todMinutes, _todHours = 0x01; // Running time (start at 1:00:00.0 AM)
    // ReSharper disable once ReplaceWithFieldKeyword
    private byte _todTenthsLatch, _todSecondsLatch, _todMinutesLatch, _todHoursLatch; // Latched display
    private byte _todTenthsAlarm, _todSecondsAlarm, _todMinutesAlarm, _todHoursAlarm; // Alarm
    private bool _todLatched;
    private bool _todWriteFreeze;

    // Serial shift register state
    private byte _sdrShiftCounter;
    private bool _sdrTransferInProgress;

    // PB6/PB7 timer output state
    private bool _pb6Toggle;
    private bool _pb7Toggle;
    private bool _pb6Pulse;
    private bool _pb7Pulse;

    // FLAG pin state
    private bool _flagPinPrevious = true;

    public uint Size => 0x100;
    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    /// <summary>
    /// External input callback for Port A pins.
    /// Returns the state of external devices connected to Port A.
    /// Bits where DDR=0 (input) will read from this source.
    /// </summary>
    public Func<byte>? ExternalPortAInput { get; set; }

    /// <summary>
    /// External input callback for Port B pins.
    /// Returns the state of external devices connected to Port B.
    /// Bits where DDR=0 (input) will read from this source.
    /// </summary>
    public Func<byte>? ExternalPortBInput { get; set; }

    /// <summary>
    /// PRA register - Data Port A with DDR masking.
    /// Read: (latch &amp; DDR) | (external &amp; ~DDR)
    /// </summary>
    public byte DataPortA
    {
        get
        {
            var externalInput = ExternalPortAInput?.Invoke() ?? 0xFF;
            return (byte)((field & DataDirectionPortA) |
                          (externalInput & ~DataDirectionPortA));
        }
        set;
    } = 0xFF;

    /// <summary>
    /// PRB Register - Data Port B with DDR masking and timer output.
    /// Read: (latch &amp; DDR) | (external &amp; ~DDR), with PB6/PB7 timer output applied.
    /// </summary>
    public byte DataPortB
    {
        get
        {
            var externalInput = ExternalPortBInput?.Invoke() ?? 0xFF;
            var result = (byte)((field & DataDirectionPortB) |
                                 (externalInput & ~DataDirectionPortB));

            // Apply PB6 timer output if enabled
            if ((ControlTimerA & TimerPbOutput) != 0)
            {
                var pb6High = (ControlTimerA & TimerPbToggle) != 0
                    ? _pb6Toggle
                    : !_pb6Pulse; // Pulse is active low

                if (pb6High)
                    result |= 0x40;
                else
                    result &= 0xBF;
            }

            // Apply PB7 timer output if enabled
            if ((ControlTimerB & TimerPbOutput) != 0)
            {
                var pb7High = (ControlTimerB & TimerPbToggle) != 0
                    ? _pb7Toggle
                    : !_pb7Pulse;

                if (pb7High)
                    result |= 0x80;
                else
                    result &= 0x7F;
            }

            return result;
        }
        set;
    } = 0xFF;

    /// <summary>
    /// DDRA register.
    /// Whether each bit of the DataPortA is read only (0) or read + write (1)
    /// </summary>
    public byte DataDirectionPortA { get; set; }

    /// <summary>
    /// DDRB register
    /// Whether each bit of the DataPortB is read only (0) or read + write (1)
    /// </summary>
    public byte DataDirectionPortB { get; set; }

    /// <summary>
    /// TALO register
    /// </summary>
    public byte TimerALowByte
    {
        get => (byte)(_timerA.Value & 0x00FF);
        set => _timerA.Latch = (ushort)((_timerA.Latch & 0xFF00) + value);
    }

    /// <summary>
    /// TAHI register
    /// </summary>
    public byte TimerAHighByte
    {
        get => (byte)((_timerA.Value & 0xFF00) >> 8);
        set
        {
            _timerA.Latch = (ushort)((_timerA.Latch & 0x00FF) + (value << 8));
            if (!_timerA.IsRunning)
            {
                _timerA.Value = _timerA.Latch;
            }
        }
    }

    /// <summary>
    /// TBLO register
    /// </summary>
    public byte TimerBLowByte
    {
        get => (byte)(_timerB.Value & 0x00FF);
        set
        {
            _timerB.Latch = (ushort)((_timerB.Latch & 0xFF00) + value);
            if (!_timerB.IsRunning)
            {
                _timerB.Value = _timerB.Latch;
            }
        }
    }

    /// <summary>
    /// TBHI register
    /// </summary>
    public byte TimerBHighByte
    {
        get => (byte)((_timerB.Value & 0xFF00) >> 8);
        set => _timerB.Latch = (ushort)((_timerB.Latch & 0x00FF) + (value << 8));
    }

    /// <summary>
    /// TOD10THS register - Tenths of a second in BCD format (0-9).
    /// Reading unlatches the display. Writing sets time or alarm based on CRB bit 7.
    /// </summary>
    public byte RealTimeClockTenthSeconds
    {
        get
        {
            byte value = _todLatched ? _todTenthsLatch : _todTenths;
            _todLatched = false;  // Reading tenths unlatches
            return value;
        }
        set
        {
            if ((ControlTimerB & TodAlarmSetMode) != 0)
            {
                _todTenthsAlarm = (byte)(value & 0x0F);
            }
            else
            {
                _todTenths = (byte)(value & 0x0F);
                _todWriteFreeze = false;  // Unfreeze TOD
            }
        }
    }

    /// <summary>
    /// TODSEC register - Seconds in BCD format (00-59).
    /// </summary>
    public byte RealTimeClockSeconds
    {
        get => _todLatched ? _todSecondsLatch : _todSeconds;
        set
        {
            if ((ControlTimerB & TodAlarmSetMode) != 0)
                _todSecondsAlarm = (byte)(value & 0x7F);
            else
                _todSeconds = (byte)(value & 0x7F);
        }
    }

    /// <summary>
    /// TODMIN register - Minutes in BCD format (00-59).
    /// </summary>
    public byte RealTimeClockMinutes
    {
        get => _todLatched ? _todMinutesLatch : _todMinutes;
        set
        {
            if ((ControlTimerB & TodAlarmSetMode) != 0)
                _todMinutesAlarm = (byte)(value & 0x7F);
            else
                _todMinutes = (byte)(value & 0x7F);
        }
    }

    /// <summary>
    /// TODHR register - Hours in BCD format (01-12). Bit 7 denotes AM (0) and PM (1).
    /// Reading latches all TOD registers. Writing freezes TOD until tenths is written.
    /// </summary>
    public byte RealTimeClockHours
    {
        get
        {
            if (!_todLatched)
            {
                // Latch all display registers
                _todTenthsLatch = _todTenths;
                _todSecondsLatch = _todSeconds;
                _todMinutesLatch = _todMinutes;
                _todHoursLatch = _todHours;
                _todLatched = true;
            }
            return _todHoursLatch;
        }
        set
        {
            if ((ControlTimerB & TodAlarmSetMode) != 0)
            {
                _todHoursAlarm = (byte)(value & 0x9F);
            }
            else
            {
                _todHours = (byte)(value & 0x9F);
                _todWriteFreeze = true;  // Freeze TOD until tenths written
            }
        }
    }

    /// <summary>
    /// SDR register - Serial Data Register.
    /// Writing in output mode starts a new 8-bit transfer clocked by Timer A underflows.
    /// </summary>
    public byte SerialShiftRegister
    {
        get;
        set
        {
            field = value;
            // In output mode, writing SDR starts a new 8-bit transfer
            if ((ControlTimerA & SerialPortMode) != 0)
            {
                _sdrShiftCounter = 0;
                _sdrTransferInProgress = true;
            }
        }
    }

    /// <summary>
    /// ICR register.
    /// Read: Returns interrupt flags (bits 0-4) with bit 7 set if IRQ occurred. Clears flags on read.
    /// Write: Bit 7=1 sets mask bits, Bit 7=0 clears mask bits specified in bits 0-4.
    /// </summary>
    public byte InterruptControlAndStatus
    {
        get
        {
            byte result = _interruptFlags;
            if ((_interruptFlags & _interruptMask) != 0)
                result |= 0x80;  // Bit 7 = IRQ occurred
            _interruptFlags = 0;  // Reading clears the flags
            return result;
        }
        set
        {
            // Bit 7 determines whether we set (1) or clear (0) the mask bits
            if ((value & 0x80) != 0)
                _interruptMask |= (byte)(value & 0x1F);
            else
                _interruptMask &= (byte)~(value & 0x1F);
        }
    }

    /// <summary>
    /// CRA register
    /// </summary>
    public byte ControlTimerA
    {
        get;
        set
        {
            _timerA.IsRunning = (value & TimerStart) != 0;

            // Bit 4: Force load - transfers latch to timer immediately (strobe, always reads 0)
            if ((value & ForceLoad) != 0)
            {
                _timerA.Value = _timerA.Latch;
                value &= unchecked((byte)~ForceLoad);  // Clear the strobe bit
            }

            field = value;
        }
    }

    /// <summary>
    /// CRB register
    /// </summary>
    public byte ControlTimerB
    {
        get;
        set
        {
            _timerB.IsRunning = (value & TimerStart) != 0;

            // Bit 4: Force load - transfers latch to timer immediately (strobe, always reads 0)
            if ((value & ForceLoad) != 0)
            {
                _timerB.Value = _timerB.Latch;
                value &= unchecked((byte)~ForceLoad);  // Clear the strobe bit
            }

            field = value;
        }
    }

    /// <summary>
    /// Returns true if an IRQ should be asserted to the CPU.
    /// Check this after RunCycle() to determine if IRQ line should be pulled low.
    /// </summary>
    public bool IrqActive => (_interruptFlags & _interruptMask) != 0;

    /// <summary>
    /// Sets the FLAG pin state. A negative edge (high to low transition)
    /// triggers an interrupt if enabled in the ICR mask.
    /// </summary>
    public void SetFlagPin(bool state)
    {
        // Detect negative edge (was high, now low)
        if (_flagPinPrevious && !state)
        {
            _interruptFlags |= IcrFlagPin;
        }
        _flagPinPrevious = state;
    }

    public void Write(ushort offset, byte value)
    {
        switch (offset & 0x0F)
        {
            case 0x00:
                DataPortA = value;
                break;

            case 0x01:
                DataPortB = value;
                break;

            case 0x02:
                DataDirectionPortA = value;
                break;

            case 0x03:
                DataDirectionPortB = value;
                break;

            case 0x04:
                TimerALowByte = value;
                break;

            case 0x05:
                TimerAHighByte = value;
                break;

            case 0x06:
                TimerBLowByte = value;
                break;

            case 0x07:
                TimerBHighByte = value;
                break;

            case 0x08:
                RealTimeClockTenthSeconds = value;
                break;

            case 0x09:
                RealTimeClockSeconds = value;
                break;

            case 0x0A:
                RealTimeClockMinutes = value;
                break;

            case 0x0B:
                RealTimeClockHours = value;
                break;

            case 0x0C:
                SerialShiftRegister = value;
                break;

            case 0x0D:
                InterruptControlAndStatus = value;
                break;

            case 0x0E:
                ControlTimerA = value;
                break;

            case 0x0F:
                ControlTimerB = value;
                break;
        }
    }

    public byte Read(ushort offset)
    {
        return (offset & 0x0F) switch
        {
            0x00 => DataPortA,
            0x01 => DataPortB,
            0x02 => DataDirectionPortA,
            0x03 => DataDirectionPortB,
            0x04 => TimerALowByte,
            0x05 => TimerAHighByte,
            0x06 => TimerBLowByte,
            0x07 => TimerBHighByte,
            0x08 => RealTimeClockTenthSeconds,
            0x09 => RealTimeClockSeconds,
            0x0A => RealTimeClockMinutes,
            0x0B => RealTimeClockHours,
            0x0C => SerialShiftRegister,
            0x0D => InterruptControlAndStatus,
            0x0E => ControlTimerA,
            0x0F => ControlTimerB,
            _ => 0  // Should never happen with & 0x0F
        };
    }

    public void RunCycle()
    {
        // Clear underflow flags and pulse states from previous cycle
        _timerA.Underflowed = false;
        _timerB.Underflowed = false;
        _pb6Pulse = false;
        _pb7Pulse = false;

        // Process Timer A
        if (_timerA.IsRunning)
        {
            // Check input mode (bit 5 of CRA) - only count on Phi2 if bit 5 is 0
            bool countOnPhi2 = (ControlTimerA & TimerInputMode) == 0;

            if (countOnPhi2)
            {
                if (_timerA.Value == 0)
                {
                    // Underflow occurred
                    _timerA.Underflowed = true;

                    // Set interrupt flag
                    _interruptFlags |= IcrTimerA;

                    // Reload from latch
                    _timerA.Value = _timerA.Latch;

                    // Handle PB6 output
                    if ((ControlTimerA & TimerPbOutput) != 0)
                    {
                        if ((ControlTimerA & TimerPbToggle) != 0)
                            _pb6Toggle = !_pb6Toggle;
                        else
                            _pb6Pulse = true;
                    }

                    // In output mode, Timer A underflows clock the serial shift register
                    if (_sdrTransferInProgress && (ControlTimerA & SerialPortMode) != 0)
                    {
                        _sdrShiftCounter++;
                        if (_sdrShiftCounter >= 8)
                        {
                            _sdrShiftCounter = 0;
                            _sdrTransferInProgress = false;
                            _interruptFlags |= IcrSerialComplete;
                        }
                    }

                    // Check for one-shot mode (bit 1 of CRA)
                    if ((ControlTimerA & TimerRunMode) != 0)
                    {
                        _timerA.IsRunning = false;
                        ControlTimerA &= unchecked((byte)~TimerStart);
                    }
                }
                else
                {
                    _timerA.Value--;
                }
            }
        }

        // Process Timer B
        if (_timerB.IsRunning)
        {
            // Determine Timer B count source from bits 5-6 of CRB
            int countMode = (ControlTimerB & TimerBCountModeMask) >> 5;
            bool shouldCount = countMode switch
            {
                0b00 => true,                    // Phi2 clock - always count
                0b01 => false,                   // CNT pin - not implemented
                0b10 => _timerA.Underflowed,     // Timer A underflow
                0b11 => _timerA.Underflowed,     // Timer A underflow while CNT high (simplified)
                _ => false
            };

            if (shouldCount)
            {
                if (_timerB.Value == 0)
                {
                    // Underflow occurred
                    _timerB.Underflowed = true;

                    // Set interrupt flag
                    _interruptFlags |= IcrTimerB;

                    // Reload from latch
                    _timerB.Value = _timerB.Latch;

                    // Handle PB7 output
                    if ((ControlTimerB & TimerPbOutput) != 0)
                    {
                        if ((ControlTimerB & TimerPbToggle) != 0)
                            _pb7Toggle = !_pb7Toggle;
                        else
                            _pb7Pulse = true;
                    }

                    // Check for one-shot mode (bit 1 of CRB)
                    if ((ControlTimerB & TimerRunMode) != 0)
                    {
                        _timerB.IsRunning = false;
                        ControlTimerB &= unchecked((byte)~TimerStart);
                    }
                }
                else
                {
                    _timerB.Value--;
                }
            }
        }

        // Process TOD clock
        _todCycleCounter++;
        int cyclesPerTick = (ControlTimerA & TodFrequencySelect) != 0
            ? CyclesPerTodTick50Hz
            : CyclesPerTodTick60Hz;

        if (_todCycleCounter >= cyclesPerTick)
        {
            _todCycleCounter = 0;
            TickTod();
        }
    }

    /// <summary>
    /// Advances the TOD clock by one tick (1/10th second)
    /// </summary>
    private void TickTod()
    {
        if (_todWriteFreeze)
            return;

        // Increment tenths (0-9)
        _todTenths = IncrementBcd(_todTenths, 0x09, out bool tenthsOverflow);

        if (tenthsOverflow)
        {
            // Increment seconds (00-59)
            _todSeconds = IncrementBcd(_todSeconds, 0x59, out bool secondsOverflow);

            if (secondsOverflow)
            {
                // Increment minutes (00-59)
                _todMinutes = IncrementBcd(_todMinutes, 0x59, out bool minutesOverflow);

                if (minutesOverflow)
                {
                    // Increment hours (12-hour format with AM/PM)
                    _todHours = IncrementBcdHours(_todHours);
                }
            }
        }

        // Check for alarm match
        if (_todTenths == _todTenthsAlarm &&
            _todSeconds == _todSecondsAlarm &&
            _todMinutes == _todMinutesAlarm &&
            _todHours == _todHoursAlarm)
        {
            _interruptFlags |= IcrTodAlarm;
        }
    }

    /// <summary>
    /// Increments a BCD value by 1, with rollover at max
    /// </summary>
    private static byte IncrementBcd(byte value, byte max, out bool overflow)
    {
        overflow = false;
        byte low = (byte)(value & 0x0F);
        byte high = (byte)((value >> 4) & 0x07);  // Mask to 3 bits for tens digit

        low++;
        if (low > 9)
        {
            low = 0;
            high++;
        }

        byte result = (byte)((high << 4) | low);
        if (result > max)
        {
            result = 0;
            overflow = true;
        }

        return result;
    }

    /// <summary>
    /// Increments the hours register in BCD with 12-hour AM/PM handling
    /// </summary>
    private static byte IncrementBcdHours(byte value)
    {
        byte amPm = (byte)(value & 0x80);
        byte hours = (byte)(value & 0x1F);  // Hours in bits 0-4 (BCD 01-12)

        // Increment hours
        byte low = (byte)(hours & 0x0F);
        byte high = (byte)((hours >> 4) & 0x01);

        low++;
        if (low > 9)
        {
            low = 0;
            high++;
        }

        hours = (byte)((high << 4) | low);

        // Handle 12-hour rollover: 12 -> 1, toggle AM/PM at 12
        if (hours == 0x12)
        {
            // Toggle AM/PM when reaching 12
            amPm ^= 0x80;
        }
        else if (hours == 0x13)
        {
            // Roll from 12 to 1
            hours = 0x01;
        }

        return (byte)(amPm | hours);
    }

    private class Timer
    {
        public bool IsRunning { get; set; }
        public ushort Value { get; set; } = 0xFFFF;
        public ushort Latch { get; set; } = 0xFFFF;
        public bool Underflowed { get; set; }
    }
}
