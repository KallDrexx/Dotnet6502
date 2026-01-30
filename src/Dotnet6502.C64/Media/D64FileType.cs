namespace Dotnet6502.C64.Media;

/// <summary>
/// CBM DOS file types as stored in the directory entry type byte (bits 0-2).
/// </summary>
public enum D64FileType : byte
{
    /// <summary>Deleted file.</summary>
    DEL = 0,

    /// <summary>Sequential file.</summary>
    SEQ = 1,

    /// <summary>Program file.</summary>
    PRG = 2,

    /// <summary>User file.</summary>
    USR = 3,

    /// <summary>Relative file.</summary>
    REL = 4
}
