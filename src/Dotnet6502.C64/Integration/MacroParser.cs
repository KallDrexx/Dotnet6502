using Microsoft.Xna.Framework.Input;

namespace Dotnet6502.C64.Integration;

/// <summary>
/// Parses macro files into a list of executable instructions.
/// </summary>
public static class MacroParser
{
    private static readonly Dictionary<char, Keys> CharKeyMappings = BuildCharKeyMappings();

    private static Dictionary<char, Keys> BuildCharKeyMappings()
    {
        var map = new Dictionary<char, Keys>();
        for (var c = 'a'; c <= 'z'; c++)
            map[c] = Keys.A + (c - 'a');
        for (var c = 'A'; c <= 'Z'; c++)
            map[c] = Keys.A + (c - 'A');
        for (var c = '0'; c <= '9'; c++)
            map[c] = Keys.D0 + (c - '0');
        map[' '] = Keys.Space;
        map[';'] = Keys.OemSemicolon;
        map['['] = Keys.OemOpenBrackets;
        map[','] = Keys.OemComma;
        return map;
    }

    private static readonly Dictionary<string, Keys> KeyNameMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Letters A-Z
        ["A"] = Keys.A,
        ["B"] = Keys.B,
        ["C"] = Keys.C,
        ["D"] = Keys.D,
        ["E"] = Keys.E,
        ["F"] = Keys.F,
        ["G"] = Keys.G,
        ["H"] = Keys.H,
        ["I"] = Keys.I,
        ["J"] = Keys.J,
        ["K"] = Keys.K,
        ["L"] = Keys.L,
        ["M"] = Keys.M,
        ["N"] = Keys.N,
        ["O"] = Keys.O,
        ["P"] = Keys.P,
        ["Q"] = Keys.Q,
        ["R"] = Keys.R,
        ["S"] = Keys.S,
        ["T"] = Keys.T,
        ["U"] = Keys.U,
        ["V"] = Keys.V,
        ["W"] = Keys.W,
        ["X"] = Keys.X,
        ["Y"] = Keys.Y,
        ["Z"] = Keys.Z,

        // Numbers 0-9
        ["0"] = Keys.D0,
        ["1"] = Keys.D1,
        ["2"] = Keys.D2,
        ["3"] = Keys.D3,
        ["4"] = Keys.D4,
        ["5"] = Keys.D5,
        ["6"] = Keys.D6,
        ["7"] = Keys.D7,
        ["8"] = Keys.D8,
        ["9"] = Keys.D9,

        // Function keys F1-F8
        ["F1"] = Keys.F1,
        ["F2"] = Keys.F2,
        ["F3"] = Keys.F3,
        ["F4"] = Keys.F4,
        ["F5"] = Keys.F5,
        ["F6"] = Keys.F6,
        ["F7"] = Keys.F7,
        ["F8"] = Keys.F8,
        ["F9"] = Keys.F9,
        ["F10"] = Keys.F10,

        // Special keys
        ["RETURN"] = Keys.Enter,
        ["SPACE"] = Keys.Space,
        ["BACK"] = Keys.Back,
        ["LEFT"] = Keys.Left,
        ["RIGHT"] = Keys.Right,
        ["UP"] = Keys.Up,
        ["DOWN"] = Keys.Down,
        ["HOME"] = Keys.Home,
        ["ESCAPE"] = Keys.Escape,
        ["BRACKET"] = Keys.OemOpenBrackets,
        ["COLON"] = Keys.OemSemicolon,
        ["COMMA"] = Keys.OemComma,

        // Modifiers
        ["LSHIFT"] = Keys.LeftShift,
        ["RSHIFT"] = Keys.RightShift,
        ["LCTRL"] = Keys.LeftControl,
        ["TAB"] = Keys.Tab,
    };

    /// <summary>
    /// Parses a macro file and returns a list of instructions.
    /// </summary>
    /// <param name="filePath">Path to the macro file.</param>
    /// <returns>List of parsed macro instructions.</returns>
    /// <exception cref="FormatException">Thrown when the file contains invalid syntax.</exception>
    public static List<MacroInstruction> ParseFile(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines, filePath);
    }

    /// <summary>
    /// Parses macro lines and returns a list of instructions.
    /// </summary>
    /// <param name="lines">Lines to parse.</param>
    /// <param name="sourceName">Source name for error messages.</param>
    /// <returns>List of parsed macro instructions.</returns>
    private static List<MacroInstruction> ParseLines(string[] lines, string sourceName = "<input>")
    {
        var instructions = new List<MacroInstruction>();

        for (var lineNumber = 1; lineNumber <= lines.Length; lineNumber++)
        {
            var line = lines[lineNumber - 1];

            // Remove comments (everything after #)
            var commentIndex = line.IndexOf('#');
            if (commentIndex >= 0)
            {
                line = line[..commentIndex];
            }

            // Trim whitespace
            line = line.Trim();

            // Skip empty lines
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            instructions.AddRange(ParseLine(line, lineNumber, sourceName));
        }

        return instructions;
    }

    private static IEnumerable<MacroInstruction> ParseLine(string line, int lineNumber, string sourceName)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new FormatException($"{sourceName}:{lineNumber}: Empty instruction");
        }

        var command = parts[0].ToLowerInvariant();

        if (command == "type")
            return ParseType(line, lineNumber, sourceName);

        return command switch
        {
            "wait" => [ParseWait(parts, lineNumber, sourceName)],
            "press" => [ParsePress(parts, lineNumber, sourceName)],
            "release" => [ParseRelease(parts, lineNumber, sourceName)],
            "quit" => [new QuitInstruction()],
            _ => throw new FormatException($"{sourceName}:{lineNumber}: Unknown command '{parts[0]}'")
        };
    }

    private static IEnumerable<MacroInstruction> ParseType(string line, int lineNumber, string sourceName)
    {
        var firstQuote = line.IndexOf('"');
        var lastQuote = line.LastIndexOf('"');
        if (firstQuote < 0 || lastQuote <= firstQuote)
            throw new FormatException($"{sourceName}:{lineNumber}: 'type' requires a quoted string argument");

        var text = line[(firstQuote + 1)..lastQuote];
        var instructions = new List<MacroInstruction>();

        foreach (var ch in text)
        {
            if (!CharKeyMappings.TryGetValue(ch, out var key))
                throw new FormatException($"{sourceName}:{lineNumber}: Unsupported character '{ch}' in type string");

            instructions.Add(new PressInstruction(key));
            instructions.Add(new WaitInstruction(2));
            instructions.Add(new ReleaseInstruction(key));
        }

        instructions.Add(new WaitInstruction(2));
        instructions.Add(new PressInstruction(Keys.Enter));
        instructions.Add(new WaitInstruction(2));
        instructions.Add(new ReleaseInstruction(Keys.Enter));
        instructions.Add(new WaitInstruction(2));
        return instructions;
    }

    private static WaitInstruction ParseWait(string[] parts, int lineNumber, string sourceName)
    {
        if (parts.Length < 2)
        {
            throw new FormatException($"{sourceName}:{lineNumber}: 'wait' requires a frame count argument");
        }

        if (!int.TryParse(parts[1], out var frames) || frames < 0)
        {
            throw new FormatException($"{sourceName}:{lineNumber}: Invalid frame count '{parts[1]}'");
        }

        return new WaitInstruction(frames);
    }

    private static PressInstruction ParsePress(string[] parts, int lineNumber, string sourceName)
    {
        if (parts.Length < 2)
        {
            throw new FormatException($"{sourceName}:{lineNumber}: 'press' requires a key name argument");
        }

        var keyName = parts[1];
        if (!KeyNameMappings.TryGetValue(keyName, out var key))
        {
            throw new FormatException($"{sourceName}:{lineNumber}: Unknown key '{keyName}'");
        }

        return new PressInstruction(key);
    }

    private static ReleaseInstruction ParseRelease(string[] parts, int lineNumber, string sourceName)
    {
        if (parts.Length < 2)
        {
            throw new FormatException($"{sourceName}:{lineNumber}: 'release' requires a key name argument");
        }

        var keyName = parts[1];
        if (!KeyNameMappings.TryGetValue(keyName, out var key))
        {
            throw new FormatException($"{sourceName}:{lineNumber}: Unknown key '{keyName}'");
        }

        return new ReleaseInstruction(key);
    }
}
