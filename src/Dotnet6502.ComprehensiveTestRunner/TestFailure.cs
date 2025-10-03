namespace Dotnet6502.ComprehensiveTestRunner;

public class TestFailure
{
    public required string Mnemonic { get; set; }
    public required string HexBytes { get; set; }

    // Initial state
    public byte InitialA { get; set; }
    public byte InitialX { get; set; }
    public byte InitialY { get; set; }
    public byte InitialP { get; set; }
    public byte InitialS { get; set; }
    public ushort InitialPc { get; set; }
    public required string InitialRam { get; set; }

    // Expected state
    public byte ExpectedA { get; set; }
    public byte ExpectedX { get; set; }
    public byte ExpectedY { get; set; }
    public byte ExpectedP { get; set; }
    public byte ExpectedS { get; set; }
    public ushort ExpectedPc { get; set; }
    public required string ExpectedRam { get; set; }

    // Actual state
    public byte ActualA { get; set; }
    public byte ActualX { get; set; }
    public byte ActualY { get; set; }
    public byte ActualP { get; set; }
    public byte ActualS { get; set; }
    public ushort ActualPc { get; set; }
    public required string ActualRam { get; set; }

    public required string ReadRamAddresses { get; set; }
    public required string ErrorMessage { get; set; }
}
