using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.C64.Integration;

/// <summary>
/// Base type for macro instructions that automate key presses.
/// </summary>
public abstract record MacroInstruction;

/// <summary>
/// Wait for a specified number of frames before executing the next instruction.
/// </summary>
public record WaitInstruction(int Frames) : MacroInstruction;

/// <summary>
/// Simulate pressing a key (key down). The key stays pressed until released.
/// </summary>
public record PressInstruction(Keys Key) : MacroInstruction;

/// <summary>
/// Simulate releasing a key (key up).
/// </summary>
public record ReleaseInstruction(Keys Key) : MacroInstruction;

/// <summary>
/// Immediately terminate the application.
/// </summary>
public record QuitInstruction : MacroInstruction;
