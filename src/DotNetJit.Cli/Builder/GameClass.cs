using System.Reflection;
using System.Reflection.Emit;

namespace DotNetJit.Cli.Builder;

public class GameClass
{
    public required TypeBuilder Type { get; init; }
    public required CpuRegisterClassBuilder Registers { get; init; }
    public required FieldInfo CpuRegistersField { get; init; }
}