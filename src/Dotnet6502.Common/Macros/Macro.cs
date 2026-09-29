using System;

namespace Dotnet6502.Common.Macros;

public class Macro
{
    public IReadOnlyDictionary<int, MacroInstruction[]> Instructions { get; }

    private Macro(IReadOnlyList<MacroInstruction> instructions)
    {
        Instructions = instructions.GroupBy(x => x.FrameNumber)
            .ToDictionary(x => x.Key, x => x.ToArray());
    }

    public static async Task<Macro> ParseAsync(Stream stream)
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
            if (parts.Length < 3)
            {
                var message = $"Line {lineNumber}: expected at least 3 comma delimted parts, but it had {parts.Length}";
                throw new InvalidOperationException(message);
            }

            if (!int.TryParse(parts[0], out var frameNumber))
            {
                var message = $"Line {lineNumber}: first section did not contain a valid fram enumber";
                throw new InvalidOperationException(message);
            }

            var rawValue = parts[2..].Aggregate((x, y) => $"{x},{y}");
            var rawType = parts[1].ToLower().Trim();
            if (rawType == "text")
            {
                // Expand each character into a sequence of down and up values
                for (var x = 0; x < rawValue.Length; x++)
                {
                    var character = rawValue[x].ToString();
                    instructions.Add(new MacroInstruction
                    {
                        FrameNumber = frameNumber,
                        Type = MacroInstructionType.ButtonDown,
                        Value = character,
                    });

                    instructions.Add(new MacroInstruction
                    {
                        FrameNumber = frameNumber + 1,
                        Type = MacroInstructionType.ButtonUp,
                        Value = character,
                    });

                    frameNumber += 2;
                }
            }
            else
            {
                var type = rawType switch
                {
                    "down" => MacroInstructionType.ButtonDown,
                    "up" => MacroInstructionType.ButtonUp,
                    _ => throw new InvalidOperationException($"Line {lineNumber}: Invalid instruction type '{parts[1]}'"),
                };

                instructions.Add(new MacroInstruction
                {
                    FrameNumber = frameNumber,
                    Type = type,
                    Value = rawValue,
                });
            }
        }

        return new Macro(instructions);
    }
}
