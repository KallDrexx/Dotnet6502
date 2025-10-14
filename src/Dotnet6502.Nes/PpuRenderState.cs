namespace Dotnet6502.Nes;

internal class PpuRenderState
{
    public enum Quadrant { TopLeft, TopRight, BottomLeft, BottomRight }

    /// <summary>
    /// The address of the current name table
    /// </summary>
    public ushort CurrentNameTableAddress { get; set; }

    /// <summary>
    /// The specific horizontal pixel in the name table
    /// </summary>
    public int PixelXInNameTable { get; set; }

    /// <summary>
    /// The specific vertical pixel in the name table
    /// </summary>
    public int PixelYInNameTable { get; set; }

    /// <summary>
    /// The name table column we are rendering
    /// </summary>
    public int TileColumn { get; set; }

    /// <summary>
    /// The name table row that we are rendering
    /// </summary>
    public int TileRow { get; set; }

    /// <summary>
    /// Byte offset from the start of the name table for the tile index
    /// </summary>
    public int TileByteOffset { get; set; }

    /// <summary>
    /// Byte offset from the start of the attribute table for the tile
    /// </summary>
    public int AttributeByteOffset { get; set; }

    /// <summary>
    /// Attributes store 4 quadrants in one byte, with the top two quadrants
    /// in the first two bit sets and the lower two quadrants in the latter two
    /// bit sets. This catalogues which quadrant we are currently in.
    /// </summary>
    public Quadrant AttributeQuadrant { get; set; }

    /// <summary>
    /// Which horizontal pixel we are in within the tile
    /// </summary>
    public int PixelXInTile { get; set; }

    /// <summary>
    /// Which vertical pixel we are in within teh tile
    /// </summary>
    public int PixelYInTile { get; set; }
}