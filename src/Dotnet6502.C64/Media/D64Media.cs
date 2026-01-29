namespace Dotnet6502.C64.Media;

/// <summary>
/// Represents a 1541 disk image
/// </summary>
public class D64Media
{
    // Based on specs from http://unusedino.de/ec64/technical/formats/d64.html

    private record Sector(byte[] Data);

    private record Track(Sector[] Sectors);

    private const int SectorSize = 256;
    private readonly List<Track> _tracks = [];

    public D64Media(FileInfo image)
    {
        if (!image.Exists)
        {
            var message = $"The file '{image}' was not found";
            throw new ArgumentException(message);
        }

        byte[] rawBytes;
        using (var stream = image.OpenRead())
        using (var memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            rawBytes = memoryStream.ToArray();
        }

        if (rawBytes.Length % SectorSize != 0)
        {
            var message = $"Expected image length to be a multiple of 256, but was '{rawBytes.Length}'";
            throw new InvalidOperationException(message);
        }

        SplitIntoTracks(rawBytes);
    }

    private void SplitIntoTracks(Span<byte> rawData)
    {
        var currentTrack = 0;
        while (!rawData.IsEmpty)
        {
            var sectorCount = currentTrack switch
            {
                < 17 => 21,
                < 24 => 19,
                < 30 => 18,
                _ => 17,
            };

            var sectors = new List<Sector>();
            for (var x = 0; x < sectorCount; x++)
            {
                var slice = rawData[..256];
                rawData = rawData[256..];

                sectors.Add(new Sector(slice.ToArray()));
            }

            _tracks.Add(new Track(sectors.ToArray()));

            currentTrack++;
        }
    }
}