namespace Dotnet6502.Nes;

/// <summary>
/// Manages the reading and writing of the NES PPU registers
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

    private static readonly IReadOnlySet<RegisterName> WritableRegisters = new HashSet<RegisterName>([
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

    private static readonly IReadOnlySet<RegisterName> ReadableRegisters = new HashSet<RegisterName>([
        RegisterName.PpuStatus,
        RegisterName.OamData,
        RegisterName.PpuData,
    ]);

    /// <summary>
    /// Controls if the next write is for the first or second bit for scroll or addr registers
    /// </summary>
    private bool _wRegister;

    public readonly Dictionary<RegisterName, ushort> RegisterValues = new();

    public void Write(ushort address, byte value)
    {
        var register = GetRegister(address);
        if (WritableRegisters.Contains(register))
        {
            RegisterValues[register] = value;
        }
    }

    public byte Read(ushort address)
    {
        var register = GetRegister(address);
        if (ReadableRegisters.Contains(register))
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
                var scrollRegister = _wRegister ? RegisterName.PpuScrollY : RegisterName.PpuScrollX;
                _wRegister = !_wRegister;
                return scrollRegister;

            case 6:
                var addrRegister = _wRegister ? RegisterName.PpuAddrByte2 : RegisterName.PpuAddrByte1;
                _wRegister = !_wRegister;
                return addrRegister;

            case 7:
                return RegisterName.PpuData;

            default:
                throw new NotSupportedException(byteNum.ToString());
        }
    }
}