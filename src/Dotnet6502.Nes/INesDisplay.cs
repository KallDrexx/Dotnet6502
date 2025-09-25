namespace Dotnet6502.Nes;

/// <summary>
/// Renders the frame to the display. Expected to be a framebuffer of 256x240 pixels.
/// </summary>
public interface INesDisplay
{
    void RenderFrame(RgbColor[] pixels);
}