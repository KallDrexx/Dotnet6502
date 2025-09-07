using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class GameClass
{
    public required TypeBuilder Type { get; init; }
    public required HardwareBuilder CpuRegisters { get; init; }
    public required FieldInfo HardwareField { get; init; }
}