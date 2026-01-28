using Dotnet6502.Common.Hardware;

namespace Dotnet6502.Common.Compilation;

/// <summary>
/// Allows wrapping a 6502 function with a .net function. The executable method that wraps
/// the 6502 function returns the address to run next. If the address is negative, then it
/// falls through to the 6502 function.
/// </summary>
public abstract class Patch
{
    /// <summary>
    /// The address of the function this patch should be applied to
    /// </summary>
    public abstract ushort FunctionEntryAddress { get; }

    public ExecutableMethod Apply(ExecutableMethod functionToWrap)
    {
        return hal =>
        {
            var nextAddress = NativeFunction(hal);
            return nextAddress < 0
                ? functionToWrap(hal)
                : nextAddress;
        };
    }

    /// <summary>
    /// The native .net code that wraps the native function
    /// </summary>
    /// <param name="hal">The 6502 HAL</param>
    /// <returns>
    /// Address of the next 6502 function to call. If negative, it falls into the wrapped function
    /// </returns>
    protected abstract int NativeFunction(Base6502Hal hal);

    /// <summary>
    /// Simulates an RTS instruction, returning the address from the top of the stack plus 1.
    /// </summary>
    protected int SimulateRts(Base6502Hal hal)
    {
        var low = hal.PopFromStack();
        var high = hal.PopFromStack();

        return ((high << 8) | low) + 1;
    }
}