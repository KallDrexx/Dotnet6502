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
    /// DDRA jegister.
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

    public void Write(ushort offset, byte value)
    {
        throw new NotImplementedException();
    }

    public byte Read(ushort offset)
    {
        throw new NotImplementedException();
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