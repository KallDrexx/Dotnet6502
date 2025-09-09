using System.Reflection;
using System.Reflection.Emit;

namespace DotNesJit.Cli.Builder;

public class GameClass
{
    public required TypeBuilder Type { get; init; }
    public required CpuRegisterClassBuilder Registers { get; init; }
    public required FieldInfo CpuRegistersField { get; init; }
}