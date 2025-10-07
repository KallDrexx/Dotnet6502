namespace Dotnet6502.Common;

public record CodeRegion(ushort BaseAddress, ReadOnlyMemory<byte> Bytes);