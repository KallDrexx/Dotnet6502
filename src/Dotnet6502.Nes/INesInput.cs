namespace Dotnet6502.Nes;

public interface INesInput
{
    ControllerState GetGamepad1State();
}