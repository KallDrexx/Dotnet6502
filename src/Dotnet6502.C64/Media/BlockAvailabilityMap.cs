namespace Dotnet6502.C64.Media;

public class BlockAvailabilityMap
{
    public record Entry(byte FreeSectorCount, bool[] UsedSectors);

    public byte FirstDirectoryTrackNumber => 18; // Ignore value in BAM
    public byte FirstDirectorySectorNumber => 1; // Ignore value in BAM

    public IReadOnlyList<Entry> TrackEntries { get; }

    public BlockAvailabilityMap(Span<byte> data)
    {
        var entryBytes = data[0x04..0x90];
        var trackEntries = new List<Entry>();
        while (!entryBytes.IsEmpty)
        {
            var sectorCount = entryBytes[0];
            var usedSectors = new bool[24];
            var sectorIndex = 0;
            for (var byteIndex = 1; byteIndex < 4; byteIndex++)
            {
                var value = entryBytes[byteIndex];
                for (var bit = 0; bit < 8; bit++)
                {
                    // 0 == used
                    usedSectors[sectorIndex] = (value & (1 << bit)) == 0;
                }

                sectorIndex++;
            }

            entryBytes = entryBytes[4..];
            trackEntries.Add(new Entry(sectorCount, usedSectors));
        }

        TrackEntries = trackEntries;
    }
}