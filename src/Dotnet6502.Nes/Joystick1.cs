using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Nes;

public class Joystick1 : IMemoryDevice
{
    private readonly INesInput _nesInput;
    private ControllerState _currentState = new();
    private int _inputBitIndex = 0;

    private readonly ControllerBits[] _bitOrder =
    [
        ControllerBits.A, ControllerBits.B, ControllerBits.Select, ControllerBits.Start, ControllerBits.Up,
        ControllerBits.Down, ControllerBits.Left, ControllerBits.Right
    ];

    public uint Size => 1;
    public ReadOnlyMemory<byte>? RawBlockFromZero => null;

    public Joystick1(INesInput nesInput)
    {
        _nesInput = nesInput;
    }

    public void Write(ushort offset, byte value)
    {
        // A write counts as a strobe, to start receiving bits
        _currentState = _nesInput.GetGamepad1State();
        _inputBitIndex = 0;
    }

    public byte Read(ushort offset)
    {
        var currentIndex = _inputBitIndex;

        _inputBitIndex++;

        return _bitOrder[currentIndex] switch
        {
            ControllerBits.A => _currentState.A ? (byte)1 : (byte)0,
            ControllerBits.B => _currentState.B ? (byte)1 : (byte)0,
            ControllerBits.Start => _currentState.Start ? (byte)1 : (byte)0,
            ControllerBits.Select => _currentState.Select ? (byte)1 : (byte)0,
            ControllerBits.Up => _currentState.Up ? (byte)1 : (byte)0,
            ControllerBits.Down => _currentState.Down ? (byte)1 : (byte)0,
            ControllerBits.Left => _currentState.Left ? (byte)1 : (byte)0,
            ControllerBits.Right => _currentState.Right ? (byte)1 : (byte)0,
            _ => 0
        };
    }
}