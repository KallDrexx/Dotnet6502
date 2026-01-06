using Dotnet6502.Common.Hardware;

namespace Dotnet6502.C64.Hardware;

/// <summary>
/// The CIA chip that the c64 uses for I/O control (MOS 6526 chip)
/// </summary>
public class ComplexInterfaceAdapter : IMemoryDevice
{
    private readonly Timer _timerA = new();
    private readonly Timer _timerB = new();

    public uint Size => 0x100;
    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    /// <summary>
    /// PRA register
    /// </summary>
    public byte DataPortA { get; set; }

    /// <summary>
    /// PRB Register
    /// </summary>
    public byte DataPortB { get; set; }

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
        get => (byte)(_timerA.Value & 0x0F);
        set => _timerA.Latch = (ushort)((_timerA.Latch & 0xF0) + value);
    }

    /// <summary>
    /// TAHI register
    /// </summary>
    public byte TimerAHighByte
    {
        get => (byte)((_timerA.Value & 0xF0) >> 8);
        set
        {
            _timerA.Latch = (ushort)((_timerA.Latch & 0x0F) + (value << 8));
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
        get => (byte)(_timerB.Value & 0x0F);
        set
        {
            _timerB.Latch = (ushort)((_timerB.Latch & 0xF0) + value);
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
        get => (byte)((_timerB.Value & 0xF0) >> 8);
        set => _timerB.Latch = (ushort)((_timerB.Latch & 0x0F) + (value << 8));
    }

    /// <summary>
    /// TOD10THS register
    /// Tenths of a second in BCD format
    /// </summary>
    public byte RealTimeClockTenthSeconds { get; set; }

    /// <summary>
    /// TODSEC register
    /// Seconds in BCD format
    /// </summary>
    public byte RealTimeClockSeconds { get; set; }

    /// <summary>
    /// TODMIN register
    /// Minutes in BCD format
    /// </summary>
    public byte RealTimeClockMinutes { get; set; }

    /// <summary>
    /// TODHR register
    /// Hours in BCD format. Bit 7 denotes AM (0) and PM (1).
    /// TODO: Writing into this register stops TOD, until TOD 10THS is read
    /// </summary>
    public byte RealTimeClockHours { get; set; }

    /// <summary>
    /// SDR register
    /// The byte within this register will be shifted bitwise to or from the SP-pin with every positive slope
    /// at the CNT-pin
    /// </summary>
    public byte SerialShiftRegister { get; set; }

    /// <summary>
    /// ICR register
    /// </summary>
    public byte InterruptControlAndStatus { get; set; }

    /// <summary>
    /// CRA register
    /// </summary>
    public byte ControlTimerA
    {
        get;
        set
        {
            field = value;
            _timerA.IsRunning = (value & 0b0000_0001) > 0;
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
            field = value;
            _timerB.IsRunning = (value & 0b0000_0001) > 0;
        }
    }

    public void Write(ushort offset, byte value)
    {
        switch (offset % 0x0F)
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

            default:
                throw new NotSupportedException(offset.ToString());
        }
    }

    public byte Read(ushort offset)
    {
        return (offset % 0x0F) switch
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
            _ => throw new NotSupportedException(offset.ToString())
        };
    }

    public void RunCycle()
    {

    }

    private class Timer
    {
        public bool IsRunning { get; set; }
        public ushort Value { get; set; } = 0xFFFF;
        public ushort Latch { get; set; } = 0xFFFF;
    }
}