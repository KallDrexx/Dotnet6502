namespace Dotnet6502.Common;

public interface IJitCompiler
{
    /// <summary>
    /// Executes the method starting at the specified address
    /// </summary>
    void RunMethod(ushort address);
}