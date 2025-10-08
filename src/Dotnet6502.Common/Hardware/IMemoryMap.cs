namespace Dotnet6502.Common.Hardware;

public interface IMemoryMap
{
    byte Read(ushort address);

    void Write(ushort address, byte value);

    IReadOnlyList<CodeRegion> GetCodeRegions();
}