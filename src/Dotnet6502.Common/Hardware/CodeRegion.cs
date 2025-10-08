namespace Dotnet6502.Common.Hardware;

public record CodeRegion(ushort BaseAddress, ReadOnlyMemory<byte> Bytes);