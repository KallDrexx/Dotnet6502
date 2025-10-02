namespace Dotnet6502.Common;

public interface IMemoryMap
{
    byte Read(ushort address);

    void Write(ushort address, byte value);
}