namespace Dotnet6502.C64.Media;

/// <summary>
/// Reads and queries a Commodore 64 D64 disk image.
/// Supports standard 35-track and extended 40-track images, with or without error bytes.
/// </summary>
public class D64Image
{
    /// <summary>Valid D64 image sizes in bytes.</summary>
    private static readonly int[] ValidSizes = [174848, 175531, 196608, 197376];

    /// <summary>Track 18, sector 0 — the BAM sector.</summary>
    private const int BamTrack = 18;
    private const int BamSector = 0;

    /// <summary>Directory chain starts at track 18, sector 1.</summary>
    private const int DirTrack = 18;
    private const int DirSector = 1;

    private const int SectorSize = 256;
    private const int DirEntrySize = 32;
    private const int DirEntriesPerSector = 8;
    private const int FilenameLength = 16;

    private readonly byte[] _imageData;

    /// <summary>
    /// Creates a new <see cref="D64Image"/> from raw image bytes.
    /// </summary>
    /// <param name="imageData">The raw D64 image data.</param>
    /// <exception cref="ArgumentException">Thrown if the data length is not a valid D64 size.</exception>
    public D64Image(byte[] imageData)
    {
        if (Array.IndexOf(ValidSizes, imageData.Length) < 0)
            throw new ArgumentException(
                $"Invalid D64 image size: {imageData.Length} bytes. " +
                $"Expected one of: {string.Join(", ", ValidSizes)}.",
                nameof(imageData));

        _imageData = imageData;
    }

    /// <summary>
    /// Loads a D64 image from a file path.
    /// </summary>
    /// <param name="path">Path to the .d64 file.</param>
    /// <returns>A new <see cref="D64Image"/> instance.</returns>
    public static D64Image Load(string path) => new(File.ReadAllBytes(path));

    /// <summary>
    /// The disk name as stored in the BAM sector (bytes $90-$9F), converted to ASCII.
    /// </summary>
    public string DiskName
    {
        get
        {
            var bam = ReadSector(BamTrack, BamSector);
            return ConvertPetsciiToAscii(bam.AsSpan(0x90, FilenameLength));
        }
    }

    /// <summary>
    /// The disk ID as stored in the BAM sector (bytes $A2-$A3), converted to ASCII.
    /// </summary>
    public string DiskId
    {
        get
        {
            var bam = ReadSector(BamTrack, BamSector);
            return ConvertPetsciiToAscii(bam.AsSpan(0xA2, 2));
        }
    }

    /// <summary>
    /// Lists all non-scratched files in the directory.
    /// Walks the directory sector chain starting at track 18, sector 1.
    /// </summary>
    /// <returns>A list of directory entries for all valid (non-scratched) files.</returns>
    public IReadOnlyList<D64DirectoryEntry> ListFiles()
    {
        var entries = new List<D64DirectoryEntry>();
        var track = DirTrack;
        var sector = DirSector;

        while (track != 0)
        {
            var sectorData = ReadSector(track, sector);

            for (var i = 0; i < DirEntriesPerSector; i++)
            {
                var offset = i * DirEntrySize;
                var typeByte = sectorData[offset + 0x02];

                // Scratched entry — skip
                if (typeByte == 0x00)
                    continue;

                var fileType = (D64FileType)(typeByte & 0x07);
                var isLocked = (typeByte & 0x40) != 0;
                var isClosed = (typeByte & 0x80) != 0;

                var startTrack = sectorData[offset + 0x03];
                var startSector = sectorData[offset + 0x04];

                var rawName = sectorData.AsSpan(offset + 0x05, FilenameLength);
                var petsciiName = TrimPetsciiPadding(rawName);
                var asciiName = ConvertPetsciiToAscii(rawName);

                var sizeLo = sectorData[offset + 0x1E];
                var sizeHi = sectorData[offset + 0x1F];
                var sizeInSectors = sizeLo | (sizeHi << 8);

                entries.Add(new D64DirectoryEntry(
                    fileType, isLocked, isClosed,
                    startTrack, startSector,
                    petsciiName, asciiName, sizeInSectors));
            }

            // Follow the chain — first two bytes of the sector
            var nextTrack = sectorData[0x00];
            var nextSector = sectorData[0x01];
            track = nextTrack;
            sector = nextSector;
        }

        return entries;
    }

    /// <summary>
    /// Reads the contents of a file identified by its ASCII name (case-insensitive).
    /// Follows the track/sector chain from the file's directory entry.
    /// </summary>
    /// <param name="asciiFilename">The ASCII filename to search for.</param>
    /// <returns>The complete file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file is not found in the directory.</exception>
    public byte[] ReadFile(string asciiFilename)
    {
        var entry = ListFiles()
            .FirstOrDefault(e => e.AsciiName.Equals(asciiFilename, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
            throw new FileNotFoundException(
                $"File '{asciiFilename}' not found on disk image.");

        var data = new List<byte>();
        var track = (int)entry.StartTrack;
        var sector = (int)entry.StartSector;

        while (track != 0)
        {
            var sectorData = ReadSector(track, sector);
            var nextTrack = sectorData[0x00];
            var nextSector = sectorData[0x01];

            if (nextTrack == 0)
            {
                // Last sector — nextSector indicates last used byte offset
                var lastByte = nextSector;
                if (lastByte >= 2)
                    data.AddRange(sectorData.AsSpan(2, lastByte - 1).ToArray());
            }
            else
            {
                // Full sector — data bytes are 2-255
                data.AddRange(sectorData.AsSpan(2, SectorSize - 2).ToArray());
            }

            track = nextTrack;
            sector = nextSector;
        }

        return data.ToArray();
    }

    /// <summary>
    /// Returns the number of sectors per track for the given track number.
    /// </summary>
    /// <param name="track">Track number (1-40).</param>
    /// <returns>The number of sectors on the specified track.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the track number is out of range.</exception>
    private static int SectorsPerTrack(int track) => track switch
    {
        >= 1 and <= 17 => 21,
        >= 18 and <= 24 => 19,
        >= 25 and <= 30 => 18,
        >= 31 and <= 40 => 17,
        _ => throw new ArgumentOutOfRangeException(nameof(track), track, "Track must be between 1 and 40.")
    };

    /// <summary>
    /// Converts a track/sector pair to a byte offset within the image data.
    /// </summary>
    private int GetSectorOffset(int track, int sector)
    {
        var offset = 0;
        for (var t = 1; t < track; t++)
            offset += SectorsPerTrack(t) * SectorSize;

        offset += sector * SectorSize;
        return offset;
    }

    /// <summary>
    /// Reads a 256-byte sector from the image at the given track and sector.
    /// </summary>
    private byte[] ReadSector(int track, int sector)
    {
        var offset = GetSectorOffset(track, sector);
        var buffer = new byte[SectorSize];
        Array.Copy(_imageData, offset, buffer, 0, SectorSize);
        return buffer;
    }

    /// <summary>
    /// Converts a span of PETSCII bytes to a trimmed ASCII string.
    /// </summary>
    private static string ConvertPetsciiToAscii(ReadOnlySpan<byte> petscii)
    {
        var chars = new char[petscii.Length];
        for (var i = 0; i < petscii.Length; i++)
            chars[i] = PetsciiToAsciiChar(petscii[i]);

        return new string(chars).TrimEnd();
    }

    /// <summary>
    /// Maps a single PETSCII byte to an ASCII character.
    /// </summary>
    private static char PetsciiToAsciiChar(byte b) => b switch
    {
        0xA0 => ' ',                           // Shifted space (padding)
        >= 0x20 and <= 0x40 => (char)b,        // Digits, punctuation, space
        >= 0x41 and <= 0x5A => (char)b,        // Uppercase A-Z (same code points)
        >= 0x61 and <= 0x7A => (char)(b - 0x20), // PETSCII lowercase → ASCII uppercase
        >= 0xC1 and <= 0xDA => (char)(b - 0x80), // PETSCII shifted uppercase → ASCII uppercase
        _ => '?'
    };

    /// <summary>
    /// Trims trailing $A0 padding bytes from a PETSCII filename and returns the result as a byte array.
    /// </summary>
    private static byte[] TrimPetsciiPadding(ReadOnlySpan<byte> raw)
    {
        var end = raw.Length;
        while (end > 0 && raw[end - 1] == 0xA0)
            end--;

        return raw[..end].ToArray();
    }
}
