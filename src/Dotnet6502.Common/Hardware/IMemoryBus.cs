using NESDecompiler.Core.Decompilation;
using System;

namespace Dotnet6502.Common.Hardware;

/// <summary>
/// Defines a component that routes memory  read and write operations for a specific
/// memory address to the correct memory device.
/// </summary>
public interface IMemoryBus
{
    /// <summary>
    /// Writes a byte at the absolute address specified
    /// </summary>
    void Write(ushort address, byte value);

    /// <summary>
    /// Reads a byte from the absolute address specified
    /// </summary>
    byte Read(ushort address);

    IReadOnlyList<CodeRegion> GetAllCodeRegions();
}
