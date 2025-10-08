using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Common.Compilation;

public delegate void ExecutableMethod(IJitCompiler jitCompiler, Base6502Hal hal);