using System;

namespace Dotnet6502.Common.Macros;

public class Macro
{
    public IReadOnlyList<MacroInstruction> Instructions { get; }

    private Macro(IReadOnlyList<MacroInstruction> instructions)
    {
        Instructions = [.. instructions.OrderBy(x => x.FrameNumber)];
    }

    public static async Task<Macro> Parse(Stream stream)
    {
        var instructions = new List<MacroInstruction>();
        var lineNumber = 0;
        using var reader = new StreamReader(stream, false);
        while (true)
        {
            lineNumber++;
            var line = await reader.ReadLineAsync();
            if (line == null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length != 3)
            {
                var message = $"Line {lineNumber}: expected 3 comma delimted parts, but it had {parts.Length}";
                throw new InvalidOperationException(message);
            }

            if (!int.TryParse(parts[0], out var frameNumber))
            {
                var message = $"Line {lineNumber}: first section did not contain a valid fram enumber";
                throw new InvalidOperationException(message);
            }

            var type = parts[1].ToLower().Trim() switch
            {
                "down" => MacroInstructionType.ButtonDown,
                "up" => MacroInstructionType.ButtonUp,
                "text" => MacroInstructionType.Text,
                _ => throw new InvalidOperationException($"Line {lineNumber}: Invalid instruction type '{parts[1]}'"),
            };

            instructions.Add(new MacroInstruction
            {
                FrameNumber = frameNumber,
                Type = type,
                Value = parts[3].Trim(),
            });
        }

        return new Macro(instructions);
    }
}
