using DotNesJit.Cli.Builder.InstructionHandlers;
using Shouldly;

namespace DotNesJit.Tests.Common.Compilation.InstructionHandlers;

public class ArithmeticHandlerTests
{
    private readonly ArithmeticHandlers _handler = new();

    [Fact]
    public void Inx_Increments_X_Register()
    {
        var runner = new InstructionTestRunner(_handler, 0xE8)
        {
            Hal =
            {
                XRegister = 5
            }
        };

        runner.RunTestMethod();

        runner.Hal.XRegister.ShouldBe((byte)6);
    }

    [Fact]
    public void Iny_Increments_X_Register()
    {
        var runner = new InstructionTestRunner(_handler, 0xC8)
        {
            Hal =
            {
                YRegister = 5
            }
        };

        runner.RunTestMethod();

        runner.Hal.XRegister.ShouldBe((byte)6);
    }
}