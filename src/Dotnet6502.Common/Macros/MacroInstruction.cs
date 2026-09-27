using System;

namespace Dotnet6502.Common.Macros;

public class MacroInstruction
{
    public required int FrameNumber { get; init; }
    public required MacroInstructionType Type { get; init; }
    public required string Value { get; init; }
 }
