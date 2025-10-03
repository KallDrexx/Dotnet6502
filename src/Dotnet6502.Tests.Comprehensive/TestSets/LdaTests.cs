namespace Dotnet6502.Tests.Comprehensive.TestSets;

public class LdaTests
{
    [Fact]
    public async Task Run_A1_Tests()
    {
        await TestCaseRunner.Run("a1.json");
    }

    [Fact]
    public async Task Run_A5_Tests()
    {
        await TestCaseRunner.Run("a5.json");
    }

    [Fact]
    public async Task Run_A9_Tests()
    {
        await TestCaseRunner.Run("a9.json");
    }

    [Fact]
    public async Task Run_Ad_Tests()
    {
        await TestCaseRunner.Run("ad.json");
    }

    [Fact]
    public async Task Run_B1_Tests()
    {
        await TestCaseRunner.Run("b1.json");
    }

    [Fact]
    public async Task Run_G5_Tests()
    {
        await TestCaseRunner.Run("b5.json");
    }

    [Fact]
    public async Task Run_B9_Tests()
    {
        await TestCaseRunner.Run("b9.json");
    }

    [Fact]
    public async Task Run_Bd_Tests()
    {
        await TestCaseRunner.Run("bd.json");
    }
}