using System.Reflection;
using System.Reflection.Emit;

namespace DotNesJit.Common.Compilation;

public class GameClass
{
    public required TypeBuilder Type { get; init; }
    public required CpuRegisterClassBuilder Registers { get; init; }
    public required FieldInfo HardwareField { get; init; }
    public Dictionary<string, MethodInfo> NesMethods { get; } = new();
}