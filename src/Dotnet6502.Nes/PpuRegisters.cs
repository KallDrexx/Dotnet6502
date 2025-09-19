using System.Collections.ObjectModel;

namespace Dotnet6502.Nes;

/// <summary>
/// NES Picture Processing Unit implementation
/// </summary>
public class PpuRegisters
{
    public enum RegisterName
    {
        PpuCtrl,
        PpuMask,
        PpuStatus,
        OamAddr,
        OamData,
        PpuScrollX,
        PpuScrollY,
        PpuAddrByte1,
        PpuAddrByte2,
        PpuData,
        OamDma,
    }

    private static readonly IReadOnlySet<RegisterName> _writableRegisters = new HashSet<RegisterName>([
        RegisterName.PpuCtrl,
        RegisterName.PpuMask,
        RegisterName.OamAddr,
        RegisterName.OamData,
        RegisterName.PpuScrollX,
        RegisterName.PpuScrollY,
        RegisterName.PpuAddrByte1,
        RegisterName.PpuAddrByte2,
        RegisterName.PpuData,
        RegisterName.OamDma,
    ]);

    private static readonly IReadOnlySet<RegisterName> _readableRegisters = new HashSet<RegisterName>([
        RegisterName.PpuStatus,
        RegisterName.OamData,
        RegisterName.PpuData,
    ]);

    private bool _scrollValueOnY;
    private bool _ppuAddrOnByte2;

    public readonly Dictionary<RegisterName, ushort> RegisterValues = new();

    public void Write(ushort address, byte value)
    {
        var register = GetRegister(address);
        if (_writableRegisters.Contains(register))
        {
            RegisterValues[register] = value;
        }
    }

    public byte Read(ushort address)
    {
        var register = GetRegister(address);
        if (_readableRegisters.Contains(register))
        {
            return (byte) RegisterValues[register];
        }

        return 0;
    }

    private RegisterName GetRegister(ushort address)
    {
        if (address == 0x4014)
        {
            return RegisterName.OamDma;
        }

        var byteNum = address % 8;
        switch (byteNum)
        {
            case 0: return RegisterName.PpuCtrl;
            case 1: return RegisterName.PpuMask;
            case 2: return RegisterName.PpuStatus;
            case 3: return RegisterName.OamAddr;
            case 4: return RegisterName.OamData;
            case 5:
                var scrollRegister = _scrollValueOnY ? RegisterName.PpuScrollY : RegisterName.PpuScrollX;
                _scrollValueOnY = !_scrollValueOnY;
                return scrollRegister;

            case 6:
                var addrRegister = _ppuAddrOnByte2 ? RegisterName.PpuAddrByte2 : RegisterName.PpuAddrByte1;
                _ppuAddrOnByte2 = !_ppuAddrOnByte2;
                return addrRegister;

            case 7:
                return RegisterName.PpuData;

            default:
                throw new NotSupportedException(byteNum.ToString());
        }
    }
}