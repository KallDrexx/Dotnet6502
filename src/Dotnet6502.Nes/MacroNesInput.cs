using System;
using Dotnet6502.Common.Macros;

namespace Dotnet6502.Nes;

public class MacroNesInput : INesInput
{
    private readonly Macro _macro;
    private readonly ControllerState _controllerState = new();

    public MacroNesInput(Macro macro)
    {
        _macro = macro;
    }

    public void UpdateForFrameNumber(int frameNumber)
    {
        if (_macro.Instructions.TryGetValue(frameNumber, out var instructions))
        {
            foreach (var instruction in instructions)
            {
                var isDown = instruction.Type switch
                {
                    MacroInstructionType.ButtonDown => true,
                    MacroInstructionType.ButtonUp => false,
                    _ => throw new NotSupportedException($"Frame {frameNumber} had an unsupported instruction: {instruction.Type}"),
                };

                switch (instruction.Value.ToLower().Trim())
                {
                    case "a":
                        _controllerState.A = isDown;
                        break;

                    case "b":
                        _controllerState.B = isDown;
                        break;

                    case "up":
                        _controllerState.Up = isDown;
                        break;

                    case "down":
                        _controllerState.Down = isDown;
                        break;

                    case "left":
                        _controllerState.Left = isDown;
                        break;

                    case "right":
                        _controllerState.Right = isDown;
                        break;

                    case "start":
                        _controllerState.Start = isDown;
                        break;

                    case "select":
                        _controllerState.Select = isDown;
                        break;

                    default:
                        throw new NotSupportedException($"Frame {frameNumber} had an unsupported value: '{instruction.Value}'");
                }
            }
        }
    }

    public ControllerState GetGamepad1State()
    {
        return _controllerState;
    }
}
