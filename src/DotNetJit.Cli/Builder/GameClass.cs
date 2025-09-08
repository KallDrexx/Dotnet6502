using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class GameClass
{
    public required TypeBuilder Type { get; init; }
    public required CpuRegisterClassBuilder CpuRegisters { get; init; }
    public required FieldInfo CpuRegistersField { get; init; }
}