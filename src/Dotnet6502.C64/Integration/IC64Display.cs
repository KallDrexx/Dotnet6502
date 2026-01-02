namespace Dotnet6502.C64.Emulation;

/// <summary>
/// Renders the frame to the display
/// </summary>
public interface IC64Display
{
    void RenderFrame(RgbColor[] pixels);
}