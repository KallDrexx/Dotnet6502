using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Common.Compilation;

// Returns an int to not have to deal with nullable value types
public delegate int ExecutableMethod(Base6502Hal hal);