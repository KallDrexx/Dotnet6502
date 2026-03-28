using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.C64.Integration;

/// <summary>
/// Executes macro instructions frame by frame, tracking simulated key states.
/// </summary>
public class MacroExecutor
{
    private readonly List<MacroInstruction> _instructions;
    private readonly HashSet<Keys> _simulatedKeys = new();
    private int _currentIndex;
    private int _waitCounter;
    private long _frameCount;

    /// <summary>
    /// Creates a new macro executor with the given instructions.
    /// </summary>
    /// <param name="instructions">The list of macro instructions to execute.</param>
    public MacroExecutor(List<MacroInstruction> instructions)
    {
        _instructions = instructions;
    }

    /// <summary>
    /// Gets whether all instructions have been executed.
    /// </summary>
    public bool IsComplete => _currentIndex >= _instructions.Count;

    /// <summary>
    /// Gets the currently simulated pressed keys.
    /// </summary>
    public IReadOnlySet<Keys> GetSimulatedKeys() => _simulatedKeys;

    /// <summary>
    /// Called each MonoGame Update cycle to advance the macro execution.
    /// </summary>
    public void OnFrame()
    {
        _frameCount++;

        // If we're waiting, decrement the counter and return
        if (_waitCounter > 0)
        {
            _waitCounter--;
            return;
        }

        // Process instructions until we hit a wait or run out
        while (_currentIndex < _instructions.Count)
        {
            var instruction = _instructions[_currentIndex];
            _currentIndex++;

            switch (instruction)
            {
                case WaitInstruction wait:
                    _waitCounter = wait.Frames - 1; // -1 because this frame counts
                    return;

                case PressInstruction press:
                    _simulatedKeys.Add(press.Key);
                    break;

                case ReleaseInstruction release:
                    _simulatedKeys.Remove(release.Key);
                    break;

                case QuitInstruction:
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
