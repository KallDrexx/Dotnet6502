namespace Dotnet6502.C64.Media;

/// <summary>
/// Represents a single 32-byte directory entry from a D64 disk image.
/// </summary>
/// <param name="FileType">The CBM file type (bits 0-2 of the type byte).</param>
/// <param name="IsLocked">True if the file is locked (bit 6 of the type byte).</param>
/// <param name="IsClosed">True if the file is properly closed; false indicates a "splat" file (bit 7 of the type byte).</param>
/// <param name="StartTrack">Track number of the first data sector.</param>
/// <param name="StartSector">Sector number of the first data sector.</param>
/// <param name="PetsciiName">Raw 16-byte PETSCII filename with trailing $A0 padding removed.</param>
/// <param name="AsciiName">ASCII-converted and trimmed filename.</param>
/// <param name="SizeInSectors">File size in sectors as stored in the directory entry.</param>
public record D64DirectoryEntry(
    D64FileType FileType,
    bool IsLocked,
    bool IsClosed,
    byte StartTrack,
    byte StartSector,
    byte[] PetsciiName,
    string AsciiName,
    int SizeInSectors);
